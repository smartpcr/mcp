
namespace Shared.Contracts.Messages;

using System;
using OrderSystem.Contracts.Messages;

// Product/Catalog Commands
public record CreateProduct(string ProductId, string Name, decimal Price, int InitialStock, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

public record ReserveStock(string ProductId, string OrderId, int Quantity, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

public record ReleaseStock(string ProductId, string OrderId, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

public record ReplenishStock(string ProductId, int Quantity, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}


// Product Events
public interface IProductEvent : IEvent { string ProductId { get; } }

public record ProductCreatedEvent(string ProductId, string Name, decimal Price, int InitialStock, DateTime CreatedAt) : IProductEvent;
public record StockReservedEvent(string ProductId, string OrderId, int Quantity, DateTime ReservedAt) : IProductEvent;
public record StockReleasedEvent(string ProductId, string OrderId, int Quantity, DateTime ReleasedAt) : IProductEvent;
public record StockReplenishedEvent(string ProductId, int Quantity, DateTime ReplenishedAt) : IProductEvent;

// Product Replies
public record ProductCreated(string ProductId);
public record ProductNotFound(string ProductId);
public record StockReservationResult(string ProductId, string OrderId, bool Success, string? Reason = null);
public record StockAvailabilityResult(string ProductId, bool IsAvailable, int AvailableQuantity);