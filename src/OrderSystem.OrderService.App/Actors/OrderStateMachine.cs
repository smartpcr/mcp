// -----------------------------------------------------------------------
// <copyright file="OrderStateMachine.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.OrderService.App.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Stateless;
    using OrderSystem.Contracts.Models;

    public enum OrderState
    {
        Initial,
        AwaitingStockReservation,
        StockReserved,
        AwaitingPayment,
        PaymentCompleted,
        AwaitingShipment,
        Shipped,
        Delivered,
        Failed,
        Cancelled
    }

    public enum OrderTrigger
    {
        OrderCreated,
        AllStockReserved,
        StockReservationFailed,
        PaymentRequested,
        PaymentSucceeded,
        PaymentFailed,
        ShipmentRequested,
        ShipmentScheduled,
        ShipmentFailed,
        Delivered,
        CancelOrder
    }

    public class OrderStateMachine
    {
        private readonly StateMachine<OrderState, OrderTrigger> stateMachine;
        private readonly OrderSagaData sagaData;

        public OrderStateMachine(OrderSagaData sagaData)
        {
            this.sagaData = sagaData;
            this.stateMachine = new StateMachine<OrderState, OrderTrigger>(
                () => this.GetCurrentState(),
                state => this.SetCurrentState(state)
            );

            ConfigureStateMachine();
        }

        public StateMachine<OrderState, OrderTrigger> Machine => stateMachine;

        public async Task FireAsync(OrderTrigger trigger, object? data = null)
        {
            await stateMachine.FireAsync(trigger);
        }

        public bool CanFire(OrderTrigger trigger)
        {
            return stateMachine.CanFire(trigger);
        }

        public OrderState CurrentState => stateMachine.State;

        private OrderState GetCurrentState()
        {
            return Enum.TryParse<OrderState>(sagaData.CurrentState, out var state) 
                ? state 
                : OrderState.Initial;
        }

        private void SetCurrentState(OrderState state)
        {
            sagaData.CurrentState = state.ToString();
        }

        private void ConfigureStateMachine()
        {
            // Initial state transitions
            stateMachine.Configure(OrderState.Initial)
                .Permit(OrderTrigger.OrderCreated, OrderState.AwaitingStockReservation)
                .OnEntryFromAsync(OrderTrigger.OrderCreated, async () => await OnOrderCreated());

            // Stock reservation phase
            stateMachine.Configure(OrderState.AwaitingStockReservation)
                .Permit(OrderTrigger.AllStockReserved, OrderState.StockReserved)
                .Permit(OrderTrigger.StockReservationFailed, OrderState.Failed)
                .Permit(OrderTrigger.CancelOrder, OrderState.Cancelled)
                .OnEntryFromAsync(OrderTrigger.AllStockReserved, async () => await OnStockReserved())
                .OnEntryFromAsync(OrderTrigger.StockReservationFailed, async () => await OnStockReservationFailed());

            // Payment phase
            stateMachine.Configure(OrderState.StockReserved)
                .Permit(OrderTrigger.PaymentRequested, OrderState.AwaitingPayment)
                .Permit(OrderTrigger.CancelOrder, OrderState.Cancelled)
                .OnEntryAsync(async () => await OnPaymentRequested());

            stateMachine.Configure(OrderState.AwaitingPayment)
                .Permit(OrderTrigger.PaymentSucceeded, OrderState.PaymentCompleted)
                .Permit(OrderTrigger.PaymentFailed, OrderState.Failed)
                .Permit(OrderTrigger.CancelOrder, OrderState.Cancelled)
                .OnEntryFromAsync(OrderTrigger.PaymentSucceeded, async () => await OnPaymentSucceeded())
                .OnEntryFromAsync(OrderTrigger.PaymentFailed, async () => await OnPaymentFailed());

            // Shipment phase
            stateMachine.Configure(OrderState.PaymentCompleted)
                .Permit(OrderTrigger.ShipmentRequested, OrderState.AwaitingShipment)
                .Permit(OrderTrigger.CancelOrder, OrderState.Cancelled)
                .OnEntryAsync(async () => await OnShipmentRequested());

            stateMachine.Configure(OrderState.AwaitingShipment)
                .Permit(OrderTrigger.ShipmentScheduled, OrderState.Shipped)
                .Permit(OrderTrigger.ShipmentFailed, OrderState.Failed)
                .Permit(OrderTrigger.CancelOrder, OrderState.Cancelled)
                .OnEntryFromAsync(OrderTrigger.ShipmentScheduled, async () => await OnShipmentScheduled())
                .OnEntryFromAsync(OrderTrigger.ShipmentFailed, async () => await OnShipmentFailed());

            // Delivery phase
            stateMachine.Configure(OrderState.Shipped)
                .Permit(OrderTrigger.Delivered, OrderState.Delivered)
                .OnEntryFromAsync(OrderTrigger.Delivered, async () => await OnDelivered());

            // Final states
            stateMachine.Configure(OrderState.Delivered)
                .OnEntry(() => OnOrderCompleted());

            stateMachine.Configure(OrderState.Failed)
                .OnEntryAsync(async () => await OnOrderFailed());

            stateMachine.Configure(OrderState.Cancelled)
                .OnEntryAsync(async () => await OnOrderCancelled());
        }

        // State entry actions
        private async Task OnOrderCreated()
        {
            sagaData.CreatedAt = DateTime.UtcNow;
            sagaData.LastUpdated = DateTime.UtcNow;
            await sagaData.RequestStockReservations();
        }

        private async Task OnStockReserved()
        {
            sagaData.LastUpdated = DateTime.UtcNow;
            await sagaData.RequestPaymentProcessing();
        }

        private async Task OnStockReservationFailed()
        {
            sagaData.LastUpdated = DateTime.UtcNow;
            await sagaData.HandleFailureCompensation();
        }

        private async Task OnPaymentRequested()
        {
            sagaData.LastUpdated = DateTime.UtcNow;
            await Task.CompletedTask;
        }

        private async Task OnPaymentSucceeded()
        {
            sagaData.LastUpdated = DateTime.UtcNow;
            await sagaData.RequestShipment();
        }

        private async Task OnPaymentFailed()
        {
            sagaData.LastUpdated = DateTime.UtcNow;
            await sagaData.HandleFailureCompensation();
        }

        private async Task OnShipmentRequested()
        {
            sagaData.LastUpdated = DateTime.UtcNow;
            await Task.CompletedTask;
        }

        private async Task OnShipmentScheduled()
        {
            sagaData.LastUpdated = DateTime.UtcNow;
            await Task.CompletedTask;
        }

        private async Task OnShipmentFailed()
        {
            sagaData.LastUpdated = DateTime.UtcNow;
            await sagaData.HandleFailureCompensation();
        }

        private async Task OnDelivered()
        {
            sagaData.LastUpdated = DateTime.UtcNow;
            sagaData.Status = OrderStatus.Delivered;
            await Task.CompletedTask;
        }

        private void OnOrderCompleted()
        {
            sagaData.LastUpdated = DateTime.UtcNow;
            sagaData.Status = OrderStatus.Delivered;
        }

        private async Task OnOrderFailed()
        {
            sagaData.LastUpdated = DateTime.UtcNow;
            sagaData.Status = OrderStatus.PaymentFailed;
            await Task.CompletedTask;
        }

        private async Task OnOrderCancelled()
        {
            sagaData.LastUpdated = DateTime.UtcNow;
            sagaData.Status = OrderStatus.Cancelled;
            await sagaData.HandleCancellationCompensation();
        }
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