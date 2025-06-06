// -----------------------------------------------------------------------
// <copyright file="ChoreographyMessages.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.OrderService.App.Actors
{
    using System;
    using System.Collections.Generic;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.Contracts.Models;

    // Command message types for choreography
    public record ReserveStock(string ProductId, string OrderId, int Quantity, string CorrelationId) : ICommand;
    public record ReleaseStock(string ProductId, string OrderId, string CorrelationId) : ICommand;
    public record ProcessPayment(string PaymentId, string OrderId, string CustomerId, decimal Amount, PaymentMethod Method, string CorrelationId) : ICommand;
    public record RefundPayment(string PaymentId, decimal? Amount, string CorrelationId) : ICommand;
    public record CreateShipment(string ShipmentId, string OrderId, List<OrderItem> Items, OrderSystem.Contracts.Models.Address Address, string CorrelationId) : ICommand;

    // Event types for choreography
    public record StockReservedEvent(string ProductId, string OrderId, int Quantity, DateTime ReservedAt) : IEvent;
    public record StockReservationFailedEvent(string ProductId, string OrderId, string Reason, DateTime FailedAt) : IEvent;
    public record PaymentSucceededEvent(string PaymentId, string TransactionId, string GatewayResponse, DateTime ProcessedAt) : IEvent;
    public record PaymentFailedEvent(string PaymentId, string Reason, DateTime ProcessedAt) : IEvent;
    public record ShipmentScheduledEvent(string ShipmentId, string TrackingNumber, string CarrierId, DateTime EstimatedDelivery, DateTime ScheduledAt) : IEvent;
    public record ShipmentFailedEvent(string ShipmentId, string Reason, DateTime FailedAt) : IEvent;
}