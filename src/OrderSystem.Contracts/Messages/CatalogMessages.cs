// -----------------------------------------------------------------------
// <copyright file="CatalogMessages.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Shared.Contracts.Messages
{
    using Shared.Contracts.Models;

    // Commands
    public record CheckAvailability(
        string ProductId,
        int Quantity,
        string CorrelationId = null!) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }

    public record ReserveItems(
        string OrderId,
        List<OrderItem> Items,
        string CorrelationId = null!) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }

    public record ReleaseReservation(
        string OrderId,
        List<OrderItem> Items,
        string CorrelationId = null!) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }

    public record UpdateStock(
        string ProductId,
        int Quantity,
        string CorrelationId = null!) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }

    // Events
    public record ItemsAvailable(
        string ProductId,
        int RequestedQuantity,
        int AvailableQuantity,
        string CorrelationId,
        DateTime Timestamp = default) : IEvent
    {
        public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
    }

    public record ItemsReserved(
        string OrderId,
        string ReservationId,
        List<OrderItem> Items,
        string CorrelationId,
        DateTime Timestamp = default) : IEvent
    {
        public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
    }

    public record ItemsUnavailable(
        string ProductId,
        int RequestedQuantity,
        int AvailableQuantity,
        string CorrelationId,
        DateTime Timestamp = default) : IEvent
    {
        public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
    }

    public record ReservationReleased(
        string OrderId,
        string ReservationId,
        List<OrderItem> Items,
        string CorrelationId,
        DateTime Timestamp = default) : IEvent
    {
        public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
    }
}