using Shared.Contracts.Models;

namespace Shared.Contracts.Messages;

// Shipment Commands
public record CreateShipment(string ShipmentId, string OrderId, List<OrderItem> Items, Address Address, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

public record UpdateShipmentStatus(string ShipmentId, string Status, string Location, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

// Shipment Events
public interface IShipmentEvent : IEvent { string ShipmentId { get; } }

public record ShipmentCreatedEvent(string ShipmentId, string OrderId, List<OrderItem> Items, Address Address, DateTime CreatedAt) : IShipmentEvent;
public record ShipmentScheduledEvent(string ShipmentId, string OrderId, string TrackingNumber, string CarrierId, DateTime EstimatedDelivery, DateTime ScheduledAt) : IShipmentEvent;
public record ShipmentDispatchedEvent(string ShipmentId, DateTime DispatchedAt, string Location) : IShipmentEvent;
public record ShipmentDeliveredEvent(string ShipmentId, DateTime DeliveredAt, string Location) : IShipmentEvent;
public record ShipmentFailedEvent(string ShipmentId, string OrderId, string Reason, DateTime FailedAt) : IShipmentEvent;

// Shipment Replies
public record ShipmentCreated(string ShipmentId, string OrderId);
public record ShipmentResult(string ShipmentId, bool Success, string? Reason = null);

// External service interfaces
public interface IShippingService
{
    Task<ShippingResult> ScheduleShipmentAsync(string shipmentId, List<OrderItem> items, Address address);
}

public record ShippingResult(bool Success, string? TrackingNumber = null, string? CarrierId = null, DateTime? EstimatedDelivery = null, string? Reason = null);