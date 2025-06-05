// -----------------------------------------------------------------------
// <copyright file="OrderMessages.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Shared.Contracts.Messages
{
    using System;
    using System.Collections.Generic;
    using Shared.Contracts.Models;

    // Order Commands
    public record CreateOrder(string OrderId, string CustomerId, List<OrderItem> Items, Address ShippingAddress, string? CorrelationId = null) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }

    public record CancelOrder(string OrderId, string Reason, string? CorrelationId = null) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }

    public record GetOrderStatus(string OrderId, string? CorrelationId = null) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }

    // Saga coordination messages - internal to Order Actor
    public record SagaTimeout(string OrderId);

    // Order Events
    public interface IOrderEvent : IEvent
    {
        string OrderId { get; }
    }

    public record OrderCreatedEvent(string OrderId, string CustomerId, List<OrderItem> Items, Address ShippingAddress, decimal TotalAmount, DateTime CreatedAt) : IOrderEvent;

    public record OrderCancelledEvent(string OrderId, string Reason, DateTime CancelledAt) : IOrderEvent;

    public record OrderCompletedEvent(string OrderId, DateTime CompletedAt) : IOrderEvent;

    // Order Replies
    public record OrderCreated(string OrderId, OrderStatus Status);

    public record OrderCancelled(string OrderId, string Reason);

    public record OrderStatusResult(string OrderId, OrderStatus Status, List<string> Events);
}