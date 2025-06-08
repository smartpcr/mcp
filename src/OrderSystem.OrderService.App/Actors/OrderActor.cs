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
    using OrderSystem.Contracts.Models;
    using Shared.Contracts.Messages;

    public class OrderActor : ReceivePersistentActor
    {
        private OrderStateMachine? stateMachine;
        private OrderSagaData sagaData;
        private readonly string orderId;
        private readonly HashSet<string> processedCommands = new();
        private readonly ILoggingAdapter log;

        public OrderActor(string orderId)
        {
            this.log = Context.GetLogger();
            this.orderId = orderId;
            this.sagaData = new OrderSagaData
            {
                CorrelationId = Guid.NewGuid(),
                OrderId = orderId,
                ActorContext = Context,
                CurrentState = OrderState.Initial.ToString()
            };
            this.stateMachine = new OrderStateMachine(this.sagaData);

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
                    this.stateMachine = new OrderStateMachine(this.sagaData);
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

                // Fire the state machine trigger to transition to AwaitingStockReservation
                await this.stateMachine!.FireAsync(OrderTrigger.OrderCreated).ConfigureAwait(false);
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

                await this.stateMachine!.FireAsync(OrderTrigger.CancelOrder).ConfigureAwait(false);
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
                await this.stateMachine!.FireAsync(OrderTrigger.AllStockReserved).ConfigureAwait(false);
                // State machine will handle payment request automatically
                await this.stateMachine!.FireAsync(OrderTrigger.PaymentRequested).ConfigureAwait(false);
            }
        }

        private async Task Handle(StockReservationFailedEvent evt)
        {
            if (evt.OrderId != this.orderId) return;

            await this.stateMachine!.FireAsync(OrderTrigger.StockReservationFailed).ConfigureAwait(false);
            this.UpdateOrderStatus(OrderStatus.OutOfStock, $"Stock reservation failed: {evt.Reason}");
        }

        private async Task Handle(PaymentSucceededEvent evt)
        {
            if (evt.PaymentId != this.sagaData.PaymentId) return;

            await this.stateMachine!.FireAsync(OrderTrigger.PaymentSucceeded).ConfigureAwait(false);
            // State machine will handle shipment request automatically
            await this.stateMachine!.FireAsync(OrderTrigger.ShipmentRequested).ConfigureAwait(false);
        }

        private async Task Handle(PaymentFailedEvent evt)
        {
            if (evt.PaymentId != this.sagaData.PaymentId) return;

            await this.stateMachine!.FireAsync(OrderTrigger.PaymentFailed).ConfigureAwait(false);
        }

        private async Task Handle(ShipmentScheduledEvent evt)
        {
            if (evt.ShipmentId != this.sagaData.ShipmentId) return;

            await this.stateMachine!.FireAsync(OrderTrigger.ShipmentScheduled).ConfigureAwait(false);
        }

        private async Task Handle(ShipmentFailedEvent evt)
        {
            if (evt.ShipmentId != this.sagaData.ShipmentId) return;

            await this.stateMachine!.FireAsync(OrderTrigger.ShipmentFailed).ConfigureAwait(false);
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


