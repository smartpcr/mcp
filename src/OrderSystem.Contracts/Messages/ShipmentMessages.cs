// -----------------------------------------------------------------------
// <copyright file="ShipmentMessages.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using OrderSystem.Contracts.Models;

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
    public record ShipmentStatusUpdatedEvent(string ShipmentId, string Status, string Location, DateTime UpdatedAt) : IShipmentEvent;
    public record ShipmentDispatchedEvent(string ShipmentId, DateTime DispatchedAt, string Location) : IShipmentEvent;
    public record ShipmentDeliveredEvent(string ShipmentId, string OrderId, string ReceivedBy, DateTime DeliveredAt) : IShipmentEvent;
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
}