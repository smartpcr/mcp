// -----------------------------------------------------------------------
// <copyright file="OrderMessages.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Core.Messages
{
    using OrderSystem.Core.Models;

    // Commands
    public record CreateOrder(
        string CustomerId,
        List<OrderItem> Items,
        Address ShippingAddress,
        string CorrelationId = null!) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }

    public record UpdateOrderStatus(
        string OrderId,
        OrderStatus Status,
        string CorrelationId = null!) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }

    public record CancelOrder(
        string OrderId,
        string Reason,
        string CorrelationId = null!) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }

    // Events
    public record OrderCreated(
        string OrderId,
        string CustomerId,
        List<OrderItem> Items,
        decimal TotalAmount,
        Address ShippingAddress,
        string CorrelationId,
        DateTime Timestamp = default) : IEvent
    {
        public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
    }

    public record OrderStatusUpdated(
        string OrderId,
        OrderStatus PreviousStatus,
        OrderStatus NewStatus,
        string CorrelationId,
        DateTime Timestamp = default) : IEvent
    {
        public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
    }

    public record OrderCancelled(
        string OrderId,
        string Reason,
        string CorrelationId,
        DateTime Timestamp = default) : IEvent
    {
        public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
    }
}