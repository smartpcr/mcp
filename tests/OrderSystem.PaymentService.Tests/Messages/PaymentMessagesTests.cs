// -----------------------------------------------------------------------
// <copyright file="PaymentMessagesTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.PaymentService.Tests.Messages
{
    using System;
    using FluentAssertions;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.Contracts.Models;
    using Xunit;

    public class PaymentMessagesTests
    {
        [Fact]
        public void ProcessPayment_ShouldGenerateCorrelationId()
        {
            // Arrange & Act
            var command = new ProcessPayment("pay-123", "order-456", "cust-789", 99.99m, new PaymentMethod("CreditCard"));

            // Assert
            command.PaymentId.Should().Be("pay-123");
            command.OrderId.Should().Be("order-456");
            command.CustomerId.Should().Be("cust-789");
            command.Amount.Should().Be(99.99m);
            command.CorrelationId.Should().NotBeNullOrEmpty();
            Guid.TryParse(command.CorrelationId, out _).Should().BeTrue();
        }

        [Fact]
        public void ProcessPayment_ShouldUseProvidedCorrelationId()
        {
            // Arrange
            var correlationId = "custom-correlation-456";

            // Act
            var command = new ProcessPayment("pay-123", "order-456", "cust-789", 99.99m, new PaymentMethod("CreditCard"), correlationId);

            // Assert
            command.CorrelationId.Should().Be(correlationId);
        }

        [Fact]
        public void RefundPayment_ShouldCreateCorrectly()
        {
            // Arrange & Act
            var command = new RefundPayment("pay-123", 50.00m);

            // Assert
            command.PaymentId.Should().Be("pay-123");
            command.Amount.Should().Be(50.00m);
            command.CorrelationId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void PaymentInitiatedEvent_ShouldCreateCorrectly()
        {
            // Arrange
            var initiatedAt = DateTime.UtcNow;
            var paymentMethod = new PaymentMethod("CreditCard");

            // Act
            var evt = new PaymentInitiatedEvent("pay-123", "order-456", "cust-789", 99.99m, paymentMethod, initiatedAt);

            // Assert
            evt.PaymentId.Should().Be("pay-123");
            evt.OrderId.Should().Be("order-456");
            evt.CustomerId.Should().Be("cust-789");
            evt.Amount.Should().Be(99.99m);
            evt.InitiatedAt.Should().Be(initiatedAt);
            evt.Should().BeAssignableTo<IPaymentEvent>();
        }

        [Fact]
        public void PaymentSucceededEvent_ShouldCreateCorrectly()
        {
            // Arrange
            var processedAt = DateTime.UtcNow;

            // Act
            var evt = new PaymentSucceededEvent("pay-123", "order-456", "txn-789", "Gateway response", processedAt);

            // Assert
            evt.PaymentId.Should().Be("pay-123");
            evt.OrderId.Should().Be("order-456");
            evt.TransactionId.Should().Be("txn-789");
            evt.GatewayResponse.Should().Be("Gateway response");
            evt.ProcessedAt.Should().Be(processedAt);
            evt.Should().BeAssignableTo<IPaymentEvent>();
        }

        [Fact]
        public void PaymentFailedEvent_ShouldCreateCorrectly()
        {
            // Arrange
            var processedAt = DateTime.UtcNow;

            // Act
            var evt = new PaymentFailedEvent("pay-123", "order-456", "Insufficient funds", processedAt);

            // Assert
            evt.PaymentId.Should().Be("pay-123");
            evt.OrderId.Should().Be("order-456");
            evt.Reason.Should().Be("Insufficient funds");
            evt.ProcessedAt.Should().Be(processedAt);
            evt.Should().BeAssignableTo<IPaymentEvent>();
        }

        [Fact]
        public void PaymentResult_ShouldCreateSuccessResult()
        {
            // Arrange & Act
            var result = new PaymentResult("pay-123", true, "txn-456");

            // Assert
            result.PaymentId.Should().Be("pay-123");
            result.Success.Should().BeTrue();
            result.TransactionId.Should().Be("txn-456");
            result.Reason.Should().BeNull();
        }

        [Fact]
        public void PaymentResult_ShouldCreateFailureResult()
        {
            // Arrange & Act
            var result = new PaymentResult("pay-123", false, Reason: "Card declined");

            // Assert
            result.PaymentId.Should().Be("pay-123");
            result.Success.Should().BeFalse();
            result.TransactionId.Should().BeNull();
            result.Reason.Should().Be("Card declined");
        }
    }
}