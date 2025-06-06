// -----------------------------------------------------------------------
// <copyright file="ShipmentMessagesTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.ShipmentService.Tests.Messages
{
    using System;
    using FluentAssertions;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.Contracts.Models;
    using Shared.Contracts.Messages;
    using Xunit;

    public class ShipmentMessagesTests
    {
        [Fact]
        public void CreateShipment_ShouldGenerateCorrelationId()
        {
            // Arrange
            var address = new Address("123 Main St", "City", "State", "12345");

            // Act
            var command = new CreateShipment("ship-123", "order-456", address);

            // Assert
            command.ShipmentId.Should().Be("ship-123");
            command.OrderId.Should().Be("order-456");
            command.ShippingAddress.Should().Be(address);
            command.CorrelationId.Should().NotBeNullOrEmpty();
            Guid.TryParse(command.CorrelationId, out _).Should().BeTrue();
        }

        [Fact]
        public void CreateShipment_ShouldUseProvidedCorrelationId()
        {
            // Arrange
            var address = new Address("123 Main St", "City", "State", "12345");
            var correlationId = "custom-correlation-789";

            // Act
            var command = new CreateShipment("ship-123", "order-456", address, correlationId);

            // Assert
            command.CorrelationId.Should().Be(correlationId);
        }

        [Fact]
        public void UpdateShipmentStatus_ShouldCreateCorrectly()
        {
            // Arrange & Act
            var command = new UpdateShipmentStatus("ship-123", "InTransit", "Chicago Hub");

            // Assert
            command.ShipmentId.Should().Be("ship-123");
            command.Status.Should().Be("InTransit");
            command.Location.Should().Be("Chicago Hub");
            command.CorrelationId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void ShipmentCreatedEvent_ShouldCreateCorrectly()
        {
            // Arrange
            var address = new Address("123 Main St", "City", "State", "12345");
            var createdAt = DateTime.UtcNow;

            // Act
            var evt = new ShipmentCreatedEvent("ship-123", "order-456", address, createdAt);

            // Assert
            evt.ShipmentId.Should().Be("ship-123");
            evt.OrderId.Should().Be("order-456");
            evt.ShippingAddress.Should().Be(address);
            evt.CreatedAt.Should().Be(createdAt);
            evt.Should().BeAssignableTo<IShipmentEvent>();
        }

        [Fact]
        public void ShipmentStatusUpdatedEvent_ShouldCreateCorrectly()
        {
            // Arrange
            var updatedAt = DateTime.UtcNow;

            // Act
            var evt = new ShipmentStatusUpdatedEvent("ship-123", "Pending", "InTransit", "Chicago Hub", updatedAt);

            // Assert
            evt.ShipmentId.Should().Be("ship-123");
            evt.PreviousStatus.Should().Be("Pending");
            evt.NewStatus.Should().Be("InTransit");
            evt.Location.Should().Be("Chicago Hub");
            evt.UpdatedAt.Should().Be(updatedAt);
            evt.Should().BeAssignableTo<IShipmentEvent>();
        }

        [Fact]
        public void ShipmentDeliveredEvent_ShouldCreateCorrectly()
        {
            // Arrange
            var deliveredAt = DateTime.UtcNow;

            // Act
            var evt = new ShipmentDeliveredEvent("ship-123", "order-456", "John Doe", deliveredAt);

            // Assert
            evt.ShipmentId.Should().Be("ship-123");
            evt.OrderId.Should().Be("order-456");
            evt.ReceivedBy.Should().Be("John Doe");
            evt.DeliveredAt.Should().Be(deliveredAt);
            evt.Should().BeAssignableTo<IShipmentEvent>();
        }
    }
}