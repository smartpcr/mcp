// -----------------------------------------------------------------------
// <copyright file="OrderActor.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.OrderService.App.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Akka.Actor;
    using Akka.Event;
    using Akka.Persistence;
    using Automatonymous;
    using OrderSystem.Contracts.Models;
    using Shared.Contracts.Messages;

    public class OrderActor : ReceivePersistentActor
    {
        private readonly OrderStateMachine stateMachine;
        private OrderSagaData sagaData;
        private readonly string orderId;
        private readonly HashSet<string> processedCommands = new();
        private readonly ILoggingAdapter log;

        public OrderActor(string orderId)
        {
            this.log = Context.GetLogger();
            this.orderId = orderId;
            this.stateMachine = new OrderStateMachine();
            this.sagaData = new OrderSagaData
            {
                CorrelationId = Guid.NewGuid(),
                OrderId = orderId,
                ActorContext = Context
            };

            this.SetupMessageHandlers();
            this.SetupEventSubscriptions();
        }

        public override string PersistenceId => $"order-{this.orderId}";

        private void SetupMessageHandlers()
        {
            this.Command<CreateOrder>(this.Handle);
            this.Command<CancelOrder>(this.Handle);
            this.Command<UpdateOrderStatus>(this.Handle);

            // Recovery
            this.Recover<OrderCreated>(evt => this.sagaData = this.ApplyEvent(this.sagaData, evt));
            this.Recover<OrderStatusUpdated>(evt => this.sagaData = this.ApplyEvent(this.sagaData, evt));
            this.Recover<OrderCancelled>(evt => this.sagaData = this.ApplyEvent(this.sagaData, evt));
            this.Recover<SnapshotOffer>(offer =>
            {
                if (offer.Snapshot is OrderSagaData data)
                {
                    this.sagaData = data;
                    this.sagaData.ActorContext = Context;
                }
            });
        }

        private void SetupEventSubscriptions()
        {
            this.SubscribeEvent<StockReservedEvent>(this.Handle);
            this.SubscribeEvent<StockReservationFailedEvent>(this.Handle);
            this.SubscribeEvent<PaymentSucceededEvent>(this.Handle);
            this.SubscribeEvent<PaymentFailedEvent>(this.Handle);
            this.SubscribeEvent<ShipmentScheduledEvent>(this.Handle);
            this.SubscribeEvent<ShipmentFailedEvent>(this.Handle);
        }

        private void SubscribeEvent<T>(Func<T, Task> handler)
        {
            Context.System.EventStream.Subscribe(this.Self, typeof(T));
            this.CommandAsync<T>(handler);
        }

        private void Handle(CreateOrder cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                this.Sender.Tell(new OrderCreated(this.orderId, cmd.CustomerId, cmd.Items,
                    cmd.Items.Sum(i => i.UnitPrice * i.Quantity), cmd.ShippingAddress, cmd.CorrelationId));
                return;
            }

            if (!string.IsNullOrEmpty(this.sagaData.OrderId) && this.sagaData.OrderId != this.orderId)
            {
                this.Sender.Tell(new OrderCreated(this.orderId, cmd.CustomerId, cmd.Items,
                    cmd.Items.Sum(i => i.UnitPrice * i.Quantity), cmd.ShippingAddress, cmd.CorrelationId));
                return;
            }

            var totalAmount = cmd.Items.Sum(item => item.UnitPrice * item.Quantity);
            var evt = new OrderCreated(this.orderId, cmd.CustomerId, cmd.Items, totalAmount, cmd.ShippingAddress, cmd.CorrelationId);

            this.Persist(evt, async e =>
            {
                this.sagaData = this.ApplyEvent(this.sagaData, e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(e);

                // Fire the state machine event
                var orderCreatedData = new OrderCreatedData
                {
                    OrderId = e.OrderId,
                    CustomerId = e.CustomerId,
                    Items = e.Items,
                    TotalAmount = e.TotalAmount,
                    ShippingAddress = e.ShippingAddress
                };

                await this.stateMachine.RaiseEvent(this.sagaData, this.stateMachine.OrderCreatedEvent, orderCreatedData);
                Context.System.EventStream.Publish(e);
            });
        }

        private void Handle(CancelOrder cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId)) return;

            var evt = new OrderCancelled(this.orderId, cmd.Reason, cmd.CorrelationId);

            this.Persist(evt, async e =>
            {
                this.sagaData = this.ApplyEvent(this.sagaData, e);
                this.MarkCommandProcessed(cmd.CorrelationId);

                var cancellationData = new CancellationData
                {
                    OrderId = this.orderId,
                    Reason = cmd.Reason
                };

                await this.stateMachine.RaiseEvent(this.sagaData, this.stateMachine.CancelOrderEvent, cancellationData);
                Context.System.EventStream.Publish(e);
            });
        }

        // Handle events from other services (choreography)
        private async Task Handle(StockReservedEvent evt)
        {
            if (evt.OrderId != this.orderId) return;

            this.sagaData.ReservedProducts.Add(evt.ProductId);

            // Check if all items are reserved
            var allReserved = this.sagaData.Items.All(item => this.sagaData.ReservedProducts.Contains(item.ProductId));

            if (allReserved)
            {
                var stockReservationData = new StockReservationData
                {
                    OrderId = evt.OrderId,
                    ProductId = evt.ProductId,
                    IsSuccess = true
                };

                await this.stateMachine.RaiseEvent(this.sagaData, this.stateMachine.AllStockReservedEvent, stockReservationData);
                this.UpdateOrderStatus(OrderStatus.Preparing, "All stock reserved");
            }
        }

        private async Task Handle(StockReservationFailedEvent evt)
        {
            if (evt.OrderId != this.orderId) return;

            var stockReservationData = new StockReservationData
            {
                OrderId = evt.OrderId,
                ProductId = evt.ProductId,
                IsSuccess = false,
                Reason = evt.Reason
            };

            await this.stateMachine.RaiseEvent(this.sagaData, this.stateMachine.StockReservationFailedEvent, stockReservationData);
            this.UpdateOrderStatus(OrderStatus.OutOfStock, $"Stock reservation failed: {evt.Reason}");
        }

        private async Task Handle(PaymentSucceededEvent evt)
        {
            if (evt.PaymentId != this.sagaData.PaymentId) return;

            var paymentData = new PaymentData
            {
                OrderId = this.orderId,
                PaymentId = evt.PaymentId,
                IsSuccess = true,
                TransactionId = evt.TransactionId
            };

            await this.stateMachine.RaiseEvent(this.sagaData, this.stateMachine.PaymentSucceededEvent, paymentData);
            this.UpdateOrderStatus(OrderStatus.PaymentConfirmed, "Payment completed");
        }

        private async Task Handle(PaymentFailedEvent evt)
        {
            if (evt.PaymentId != this.sagaData.PaymentId) return;

            var paymentData = new PaymentData
            {
                OrderId = this.orderId,
                PaymentId = evt.PaymentId,
                IsSuccess = false,
                Reason = evt.Reason
            };

            await this.stateMachine.RaiseEvent(this.sagaData, this.stateMachine.PaymentFailedEvent, paymentData);
            this.UpdateOrderStatus(OrderStatus.PaymentFailed, $"Payment failed: {evt.Reason}");
        }

        private async Task Handle(ShipmentScheduledEvent evt)
        {
            if (evt.ShipmentId != this.sagaData.ShipmentId) return;

            var shipmentData = new ShipmentData
            {
                OrderId = this.orderId,
                ShipmentId = evt.ShipmentId,
                IsSuccess = true,
                TrackingNumber = evt.TrackingNumber
            };

            await this.stateMachine.RaiseEvent(this.sagaData, this.stateMachine.ShipmentScheduledEvent, shipmentData);
            this.UpdateOrderStatus(OrderStatus.Shipped, "Shipment scheduled");
        }

        private async Task Handle(ShipmentFailedEvent evt)
        {
            if (evt.ShipmentId != this.sagaData.ShipmentId) return;

            var shipmentData = new ShipmentData
            {
                OrderId = this.orderId,
                ShipmentId = evt.ShipmentId,
                IsSuccess = false,
                Reason = evt.Reason
            };

            await this.stateMachine.RaiseEvent(this.sagaData, this.stateMachine.ShipmentFailedEvent, shipmentData);
            this.UpdateOrderStatus(OrderStatus.ShipmentFailed, $"Shipment failed: {evt.Reason}");
        }

        private void UpdateOrderStatus(OrderStatus newStatus, string reason)
        {
            var evt = new OrderStatusUpdated(this.orderId, this.sagaData.Status, newStatus, Guid.NewGuid().ToString());

            this.Persist(evt, e =>
            {
                this.sagaData = this.ApplyEvent(this.sagaData, e);
                Context.System.EventStream.Publish(e);
            });
        }

        private OrderSagaData ApplyEvent(OrderSagaData data, OrderCreated evt)
        {
            data.OrderId = evt.OrderId;
            data.CustomerId = evt.CustomerId;
            data.Items = evt.Items;
            data.TotalAmount = evt.TotalAmount;
            data.ShippingAddress = evt.ShippingAddress;
            data.Status = OrderStatus.Pending;
            data.CreatedAt = evt.Timestamp;
            data.LastUpdated = evt.Timestamp;
            return data;
        }

        private OrderSagaData ApplyEvent(OrderSagaData data, OrderStatusUpdated evt)
        {
            data.Status = evt.NewStatus;
            data.LastUpdated = evt.Timestamp;
            return data;
        }

        private OrderSagaData ApplyEvent(OrderSagaData data, OrderCancelled evt)
        {
            data.Status = OrderStatus.Cancelled;
            data.LastUpdated = evt.Timestamp;
            return data;
        }

        private bool IsCommandProcessed(string? correlationId)
        {
            if (string.IsNullOrEmpty(correlationId)) return false;
            return this.processedCommands.Contains(correlationId);
        }

        private void MarkCommandProcessed(string? correlationId)
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                this.processedCommands.Add(correlationId);
            }
        }

        private void Handle(UpdateOrderStatus cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId)) return;

            this.UpdateOrderStatus(cmd.Status, "Manual status update");
            this.MarkCommandProcessed(cmd.CorrelationId);
        }
    }
}


