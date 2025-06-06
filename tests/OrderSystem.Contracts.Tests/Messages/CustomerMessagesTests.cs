// -----------------------------------------------------------------------
// <copyright file="CustomerMessagesTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Tests.Messages
{
    using System;
    using FluentAssertions;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.Contracts.Models;
    using Xunit;

    public class CustomerMessagesTests
    {
        [Fact]
        public void CreateCustomer_ShouldGenerateCorrelationId()
        {
            // Arrange & Act
            var command = new CreateCustomer("cust-123", "test@example.com", "John Doe");

            // Assert
            command.CustomerId.Should().Be("cust-123");
            command.Email.Should().Be("test@example.com");
            command.Name.Should().Be("John Doe");
            command.CorrelationId.Should().NotBeNullOrEmpty();
            Guid.TryParse(command.CorrelationId, out _).Should().BeTrue();
        }

        [Fact]
        public void CreateCustomer_ShouldUseProvidedCorrelationId()
        {
            // Arrange
            var correlationId = "custom-correlation-123";

            // Act
            var command = new CreateCustomer("cust-123", "test@example.com", "John Doe", correlationId);

            // Assert
            command.CorrelationId.Should().Be(correlationId);
        }

        [Fact]
        public void UpdateCustomer_ShouldCreateCorrectly()
        {
            // Arrange & Act
            var command = new UpdateCustomer("cust-456", "Jane Doe", "jane@example.com");

            // Assert
            command.CustomerId.Should().Be("cust-456");
            command.Name.Should().Be("Jane Doe");
            command.Email.Should().Be("jane@example.com");
            command.CorrelationId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void AddAddress_ShouldCreateWithAddress()
        {
            // Arrange
            var address = new Address("123 Main St", "City", "State", "12345");

            // Act
            var command = new AddAddress("cust-789", address);

            // Assert
            command.CustomerId.Should().Be("cust-789");
            command.Address.Should().Be(address);
            command.CorrelationId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void AddPaymentMethod_ShouldCreateWithPaymentMethod()
        {
            // Arrange
            var paymentMethod = new PaymentMethod("CreditCard", "****1234", "12/25", "John Doe");

            // Act
            var command = new AddPaymentMethod("cust-123", paymentMethod);

            // Assert
            command.CustomerId.Should().Be("cust-123");
            command.PaymentMethod.Should().Be(paymentMethod);
            command.CorrelationId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void ValidateCustomer_ShouldCreateCorrectly()
        {
            // Arrange & Act
            var command = new ValidateCustomer("cust-999");

            // Assert
            command.CustomerId.Should().Be("cust-999");
        }

        [Fact]
        public void CustomerCreatedEvent_ShouldSetTimestamp()
        {
            // Arrange
            var beforeTime = DateTime.UtcNow;

            // Act
            var evt = new CustomerCreatedEvent("cust-123", "test@example.com", "John Doe", DateTime.UtcNow);

            // Assert
            var afterTime = DateTime.UtcNow;
            evt.CustomerId.Should().Be("cust-123");
            evt.Email.Should().Be("test@example.com");
            evt.Name.Should().Be("John Doe");
            evt.CreatedAt.Should().BeOnOrAfter(beforeTime).And.BeOnOrBefore(afterTime);
        }

        [Fact]
        public void CustomerValidationResult_ValidCustomer_ShouldCreateCorrectly()
        {
            // Arrange & Act
            var result = new CustomerValidationResult("cust-123", true);

            // Assert
            result.CustomerId.Should().Be("cust-123");
            result.IsValid.Should().BeTrue();
            result.Reason.Should().BeNull();
        }

        [Fact]
        public void CustomerValidationResult_InvalidCustomer_ShouldCreateWithReason()
        {
            // Arrange & Act
            var result = new CustomerValidationResult("cust-456", false, "Customer not found");

            // Assert
            result.CustomerId.Should().Be("cust-456");
            result.IsValid.Should().BeFalse();
            result.Reason.Should().Be("Customer not found");
        }
    }
}