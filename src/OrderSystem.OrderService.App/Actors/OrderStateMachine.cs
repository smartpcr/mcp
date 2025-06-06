// -----------------------------------------------------------------------
// <copyright file="OrderStateMachine.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.OrderService.App.Actors
{
    using System;
    using System.Collections.Generic;
    using Automatonymous;
    using OrderSystem.Contracts.Models;

    public class OrderStateMachine : AutomatonymousStateMachine<OrderSagaData>
    {
        public OrderStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Initially(
                When(OrderCreatedEvent)
                    .TransitionTo(AwaitingStockReservation)
                    .Then(context => 
                    {
                        context.Instance.OrderId = context.Data.OrderId;
                        context.Instance.CustomerId = context.Data.CustomerId;
                        context.Instance.Items = context.Data.Items;
                        context.Instance.TotalAmount = context.Data.TotalAmount;
                        context.Instance.ShippingAddress = context.Data.ShippingAddress;
                        context.Instance.CreatedAt = DateTime.UtcNow;
                    })
                    .ThenAsync(async context =>
                    {
                        // Trigger stock reservation requests
                        await context.Instance.RequestStockReservations(context);
                    })
            );

            During(AwaitingStockReservation,
                When(AllStockReservedEvent)
                    .TransitionTo(StockReserved)
                    .ThenAsync(async context =>
                    {
                        // Trigger payment processing
                        await context.Instance.RequestPaymentProcessing(context);
                    }),
                When(StockReservationFailedEvent)
                    .TransitionTo(Failed)
                    .ThenAsync(async context =>
                    {
                        await context.Instance.HandleFailureCompensation(context);
                    })
            );

            During(StockReserved,
                When(PaymentRequestedEvent)
                    .TransitionTo(AwaitingPayment)
            );

            During(AwaitingPayment,
                When(PaymentSucceededEvent)
                    .TransitionTo(PaymentCompleted)
                    .ThenAsync(async context =>
                    {
                        // Trigger shipment creation
                        await context.Instance.RequestShipment(context);
                    }),
                When(PaymentFailedEvent)
                    .TransitionTo(Failed)
                    .ThenAsync(async context =>
                    {
                        await context.Instance.HandleFailureCompensation(context);
                    })
            );

            During(PaymentCompleted,
                When(ShipmentRequestedEvent)
                    .TransitionTo(AwaitingShipment)
            );

            During(AwaitingShipment,
                When(ShipmentScheduledEvent)
                    .TransitionTo(Shipped),
                When(ShipmentFailedEvent)
                    .TransitionTo(Failed)
                    .ThenAsync(async context =>
                    {
                        await context.Instance.HandleFailureCompensation(context);
                    })
            );

            During(Shipped,
                When(DeliveredEvent)
                    .TransitionTo(Delivered)
            );

            // Cancellation can happen from any state except final states
            DuringAny(
                When(CancelOrderEvent)
                    .TransitionTo(Cancelled)
                    .ThenAsync(async context =>
                    {
                        await context.Instance.HandleCancellationCompensation(context);
                    })
            );

            // State machine configured
        }

        // States
        public State AwaitingStockReservation { get; private set; }
        public State StockReserved { get; private set; }
        public State AwaitingPayment { get; private set; }
        public State PaymentCompleted { get; private set; }
        public State AwaitingShipment { get; private set; }
        public State Shipped { get; private set; }
        public State Delivered { get; private set; }
        public State Failed { get; private set; }
        public State Cancelled { get; private set; }

        // Events
        public Event<OrderCreatedData> OrderCreatedEvent { get; private set; }
        public Event<StockReservationData> AllStockReservedEvent { get; private set; }
        public Event<StockReservationData> StockReservationFailedEvent { get; private set; }
        public Event<PaymentData> PaymentRequestedEvent { get; private set; }
        public Event<PaymentData> PaymentSucceededEvent { get; private set; }
        public Event<PaymentData> PaymentFailedEvent { get; private set; }
        public Event<ShipmentData> ShipmentRequestedEvent { get; private set; }
        public Event<ShipmentData> ShipmentScheduledEvent { get; private set; }
        public Event<ShipmentData> ShipmentFailedEvent { get; private set; }
        public Event<DeliveryData> DeliveredEvent { get; private set; }
        public Event<CancellationData> CancelOrderEvent { get; private set; }
    }

    // Event Data Types
    public class OrderCreatedData
    {
        public string OrderId { get; set; } = "";
        public string CustomerId { get; set; } = "";
        public List<OrderItem> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public OrderSystem.Contracts.Models.Address ShippingAddress { get; set; } = new("", "", "", "", "");
    }

    public class StockReservationData
    {
        public string OrderId { get; set; } = "";
        public string ProductId { get; set; } = "";
        public bool IsSuccess { get; set; }
        public string Reason { get; set; } = "";
    }

    public class PaymentData
    {
        public string OrderId { get; set; } = "";
        public string PaymentId { get; set; } = "";
        public bool IsSuccess { get; set; }
        public string Reason { get; set; } = "";
        public string TransactionId { get; set; } = "";
    }

    public class ShipmentData
    {
        public string OrderId { get; set; } = "";
        public string ShipmentId { get; set; } = "";
        public bool IsSuccess { get; set; }
        public string Reason { get; set; } = "";
        public string TrackingNumber { get; set; } = "";
    }

    public class DeliveryData
    {
        public string OrderId { get; set; } = "";
        public string ShipmentId { get; set; } = "";
        public DateTime DeliveredAt { get; set; }
    }

    public class CancellationData
    {
        public string OrderId { get; set; } = "";
        public string Reason { get; set; } = "";
    }
}