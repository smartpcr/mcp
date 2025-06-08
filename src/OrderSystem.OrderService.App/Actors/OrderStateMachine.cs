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
    using OrderSystem.Contracts.Models;
    using Stateless;

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

            this.ConfigureStateMachine();
        }

        public StateMachine<OrderState, OrderTrigger> Machine => this.stateMachine;

        public async Task FireAsync(OrderTrigger trigger, object? data = null)
        {
            await this.stateMachine.FireAsync(trigger);
        }

        public bool CanFire(OrderTrigger trigger)
        {
            return this.stateMachine.CanFire(trigger);
        }

        public OrderState CurrentState => this.stateMachine.State;

        private OrderState GetCurrentState()
        {
            return Enum.TryParse<OrderState>(this.sagaData.CurrentState, out var state) 
                ? state 
                : OrderState.Initial;
        }

        private void SetCurrentState(OrderState state)
        {
            this.sagaData.CurrentState = state.ToString();
        }

        private void ConfigureStateMachine()
        {
            // Initial state transitions
            this.stateMachine.Configure(OrderState.Initial)
                .Permit(OrderTrigger.OrderCreated, OrderState.AwaitingStockReservation)
                .OnEntryFromAsync(OrderTrigger.OrderCreated, async () => await this.OnOrderCreated());

            // Stock reservation phase
            this.stateMachine.Configure(OrderState.AwaitingStockReservation)
                .Permit(OrderTrigger.AllStockReserved, OrderState.StockReserved)
                .Permit(OrderTrigger.StockReservationFailed, OrderState.Failed)
                .Permit(OrderTrigger.CancelOrder, OrderState.Cancelled)
                .OnEntryFromAsync(OrderTrigger.AllStockReserved, async () => await this.OnStockReserved())
                .OnEntryFromAsync(OrderTrigger.StockReservationFailed, async () => await this.OnStockReservationFailed());

            // Payment phase
            this.stateMachine.Configure(OrderState.StockReserved)
                .Permit(OrderTrigger.PaymentRequested, OrderState.AwaitingPayment)
                .Permit(OrderTrigger.CancelOrder, OrderState.Cancelled)
                .OnEntryAsync(async () => await this.OnPaymentRequested());

            this.stateMachine.Configure(OrderState.AwaitingPayment)
                .Permit(OrderTrigger.PaymentSucceeded, OrderState.PaymentCompleted)
                .Permit(OrderTrigger.PaymentFailed, OrderState.Failed)
                .Permit(OrderTrigger.CancelOrder, OrderState.Cancelled)
                .OnEntryFromAsync(OrderTrigger.PaymentSucceeded, async () => await this.OnPaymentSucceeded())
                .OnEntryFromAsync(OrderTrigger.PaymentFailed, async () => await this.OnPaymentFailed());

            // Shipment phase
            this.stateMachine.Configure(OrderState.PaymentCompleted)
                .Permit(OrderTrigger.ShipmentRequested, OrderState.AwaitingShipment)
                .Permit(OrderTrigger.CancelOrder, OrderState.Cancelled)
                .OnEntryAsync(async () => await this.OnShipmentRequested());

            this.stateMachine.Configure(OrderState.AwaitingShipment)
                .Permit(OrderTrigger.ShipmentScheduled, OrderState.Shipped)
                .Permit(OrderTrigger.ShipmentFailed, OrderState.Failed)
                .Permit(OrderTrigger.CancelOrder, OrderState.Cancelled)
                .OnEntryFromAsync(OrderTrigger.ShipmentScheduled, async () => await this.OnShipmentScheduled())
                .OnEntryFromAsync(OrderTrigger.ShipmentFailed, async () => await this.OnShipmentFailed());

            // Delivery phase
            this.stateMachine.Configure(OrderState.Shipped)
                .Permit(OrderTrigger.Delivered, OrderState.Delivered)
                .OnEntryFromAsync(OrderTrigger.Delivered, async () => await this.OnDelivered());

            // Final states
            this.stateMachine.Configure(OrderState.Delivered)
                .OnEntry(() => this.OnOrderCompleted());

            this.stateMachine.Configure(OrderState.Failed)
                .OnEntryAsync(async () => await this.OnOrderFailed());

            this.stateMachine.Configure(OrderState.Cancelled)
                .OnEntryAsync(async () => await this.OnOrderCancelled());
        }

        // State entry actions
        private async Task OnOrderCreated()
        {
            this.sagaData.CreatedAt = DateTime.UtcNow;
            this.sagaData.LastUpdated = DateTime.UtcNow;
            await this.sagaData.RequestStockReservations().ConfigureAwait(false);
        }

        private async Task OnStockReserved()
        {
            this.sagaData.LastUpdated = DateTime.UtcNow;
            await this.sagaData.RequestPaymentProcessing().ConfigureAwait(false);
        }

        private async Task OnStockReservationFailed()
        {
            this.sagaData.LastUpdated = DateTime.UtcNow;
            await this.sagaData.HandleFailureCompensation().ConfigureAwait(false);
        }

        private async Task OnPaymentRequested()
        {
            this.sagaData.LastUpdated = DateTime.UtcNow;
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task OnPaymentSucceeded()
        {
            this.sagaData.LastUpdated = DateTime.UtcNow;
            await this.sagaData.RequestShipment().ConfigureAwait(false);
        }

        private async Task OnPaymentFailed()
        {
            this.sagaData.LastUpdated = DateTime.UtcNow;
            await this.sagaData.HandleFailureCompensation().ConfigureAwait(false);
        }

        private async Task OnShipmentRequested()
        {
            this.sagaData.LastUpdated = DateTime.UtcNow;
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task OnShipmentScheduled()
        {
            this.sagaData.LastUpdated = DateTime.UtcNow;
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task OnShipmentFailed()
        {
            this.sagaData.LastUpdated = DateTime.UtcNow;
            await this.sagaData.HandleFailureCompensation().ConfigureAwait(false);
        }

        private async Task OnDelivered()
        {
            this.sagaData.LastUpdated = DateTime.UtcNow;
            this.sagaData.Status = OrderStatus.Delivered;
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private void OnOrderCompleted()
        {
            this.sagaData.LastUpdated = DateTime.UtcNow;
            this.sagaData.Status = OrderStatus.Delivered;
        }

        private async Task OnOrderFailed()
        {
            this.sagaData.LastUpdated = DateTime.UtcNow;
            this.sagaData.Status = OrderStatus.PaymentFailed;
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task OnOrderCancelled()
        {
            this.sagaData.LastUpdated = DateTime.UtcNow;
            this.sagaData.Status = OrderStatus.Cancelled;
            await this.sagaData.HandleCancellationCompensation().ConfigureAwait(false);
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