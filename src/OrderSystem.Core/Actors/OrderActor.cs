// -----------------------------------------------------------------------
// <copyright file="OrderActor.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Core.Actors
{
    using Akka.Actor;
    using Akka.Persistence;
    using OrderSystem.Core.Messages;
    using OrderSystem.Core.Models;

    public class OrderActor : ReceivePersistentActor
    {
        private readonly string _orderId;
        private OrderState _state;

        public OrderActor(string orderId)
        {
            this._orderId = orderId;
            this._state = new OrderState();
            
            this.SetupCommandHandlers();
            this.SetupEventHandlers();
        }

        public override string PersistenceId => $"order-{this._orderId}";

        private void SetupCommandHandlers()
        {
            this.Command<CreateOrder>(this.HandleCreateOrder);
            this.Command<UpdateOrderStatus>(this.HandleUpdateOrderStatus);
            this.Command<CancelOrder>(this.HandleCancelOrder);
            
            // External service responses
            this.Command<ItemsReserved>(this.HandleItemsReserved);
            this.Command<ItemsUnavailable>(this.HandleItemsUnavailable);
            this.Command<PaymentSucceeded>(this.HandlePaymentSucceeded);
            this.Command<PaymentFailed>(this.HandlePaymentFailed);
        }

        private void SetupEventHandlers()
        {
            this.Recover<OrderCreated>(evt => this.UpdateState(evt));
            this.Recover<OrderStatusUpdated>(evt => this.UpdateState(evt));
            this.Recover<OrderCancelled>(evt => this.UpdateState(evt));
        }

        private void HandleCreateOrder(CreateOrder cmd)
        {
            if (this._state.Status != OrderStatus.Pending && !string.IsNullOrEmpty(this._state.OrderId))
            {
                this.Sender.Tell(new OrderAlreadyExists(this._orderId, cmd.CorrelationId));
                return;
            }

            var totalAmount = cmd.Items.Sum(item => item.TotalPrice);
            var orderCreated = new OrderCreated(
                this._orderId,
                cmd.CustomerId,
                cmd.Items,
                totalAmount,
                cmd.ShippingAddress,
                cmd.CorrelationId);

            this.Persist(orderCreated, evt =>
            {
                this.UpdateState(evt);
                this.Sender.Tell(evt);
                
                // Start the order flow - check availability
                var catalogService = Context.ActorSelection("/user/catalog-service");
                foreach (var item in cmd.Items)
                {
                    catalogService.Tell(new CheckAvailability(item.ProductId, item.Quantity, cmd.CorrelationId));
                }
            });
        }

        private void HandleUpdateOrderStatus(UpdateOrderStatus cmd)
        {
            if (this._state.OrderId != cmd.OrderId)
            {
                this.Sender.Tell(new OrderNotFound(cmd.OrderId, cmd.CorrelationId));
                return;
            }

            var statusUpdated = new OrderStatusUpdated(
                cmd.OrderId,
                this._state.Status,
                cmd.Status,
                cmd.CorrelationId);

            this.Persist(statusUpdated, evt =>
            {
                this.UpdateState(evt);
                this.Sender.Tell(evt);
            });
        }

        private void HandleCancelOrder(CancelOrder cmd)
        {
            if (this._state.OrderId != cmd.OrderId)
            {
                this.Sender.Tell(new OrderNotFound(cmd.OrderId, cmd.CorrelationId));
                return;
            }

            var orderCancelled = new OrderCancelled(cmd.OrderId, cmd.Reason, cmd.CorrelationId);

            this.Persist(orderCancelled, evt =>
            {
                this.UpdateState(evt);
                this.Sender.Tell(evt);
                
                // Release any reservations
                if (this._state.ReservationId != null)
                {
                    var catalogService = Context.ActorSelection("/user/catalog-service");
                    catalogService.Tell(new ReleaseReservation(cmd.OrderId, this._state.Items, cmd.CorrelationId));
                }
            });
        }

        private void HandleItemsReserved(ItemsReserved evt)
        {
            // Items successfully reserved, proceed with payment
            this._state = this._state with { ReservationId = evt.ReservationId };
            
            var paymentService = Context.ActorSelection("/user/payment-service");
            paymentService.Tell(new ProcessPayment(
                this._state.OrderId!,
                this._state.CustomerId!,
                this._state.TotalAmount,
                "CreditCard", // Default payment method
                evt.CorrelationId));
        }

        private void HandleItemsUnavailable(ItemsUnavailable evt)
        {
            // Items not available, cancel the order
            var orderCancelled = new OrderCancelled(this._state.OrderId!, "Items unavailable", evt.CorrelationId);
            
            this.Persist(orderCancelled, cancelEvt =>
            {
                this.UpdateState(cancelEvt);
                Context.Parent.Tell(cancelEvt);
            });
        }

        private void HandlePaymentSucceeded(PaymentSucceeded evt)
        {
            var statusUpdated = new OrderStatusUpdated(
                this._state.OrderId!,
                this._state.Status,
                OrderStatus.PaymentConfirmed,
                evt.CorrelationId);

            this.Persist(statusUpdated, statusEvt =>
            {
                this.UpdateState(statusEvt);
                Context.Parent.Tell(statusEvt);
                
                // Create shipment
                var shipmentService = Context.ActorSelection("/user/shipment-service");
                shipmentService.Tell(new CreateShipment(
                    this._state.OrderId!,
                    this._state.Items,
                    this._state.ShippingAddress!,
                    evt.CorrelationId));
            });
        }

        private void HandlePaymentFailed(PaymentFailed evt)
        {
            var orderCancelled = new OrderCancelled(this._state.OrderId!, "Payment failed", evt.CorrelationId);
            
            this.Persist(orderCancelled, cancelEvt =>
            {
                this.UpdateState(cancelEvt);
                Context.Parent.Tell(cancelEvt);
                
                // Release reservations
                if (this._state.ReservationId != null)
                {
                    var catalogService = Context.ActorSelection("/user/catalog-service");
                    catalogService.Tell(new ReleaseReservation(this._state.OrderId!, this._state.Items, evt.CorrelationId));
                }
            });
        }

        private void UpdateState(IEvent evt)
        {
            this._state = evt switch
            {
                OrderCreated e => this._state with
                {
                    OrderId = e.OrderId,
                    CustomerId = e.CustomerId,
                    Items = e.Items,
                    TotalAmount = e.TotalAmount,
                    ShippingAddress = e.ShippingAddress,
                    Status = OrderStatus.Pending
                },
                OrderStatusUpdated e => this._state with { Status = e.NewStatus },
                OrderCancelled => this._state with { Status = OrderStatus.Cancelled },
                _ => this._state
            };
        }
    }

    // State record
    public record OrderState(
        string? OrderId = null,
        string? CustomerId = null,
        List<OrderItem> Items = null!,
        decimal TotalAmount = 0,
        Models.Address? ShippingAddress = null,
        OrderStatus Status = OrderStatus.Pending,
        string? ReservationId = null)
    {
        public List<OrderItem> Items { get; init; } = Items ?? new List<OrderItem>();
    }

    // Response messages
    public record OrderAlreadyExists(string OrderId, string CorrelationId) : IEvent
    {
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record OrderNotFound(string OrderId, string CorrelationId) : IEvent
    {
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    // Missing message definitions for compilation
    public record CreateShipment(
        string OrderId,
        List<OrderItem> Items,
        Models.Address ShippingAddress,
        string CorrelationId) : ICommand;
}