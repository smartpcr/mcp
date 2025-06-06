// -----------------------------------------------------------------------
// <copyright file="CustomerStateTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Tests.Models
{
    using FluentAssertions;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.Contracts.Models;
    using Xunit;

    public class CustomerStateTests
    {
        [Fact]
        public void DefaultCustomerState_ShouldHaveEmptyValues()
        {
            // Act
            var state = new CustomerState();

            // Assert
            state.CustomerId.Should().BeEmpty();
            state.Email.Should().BeEmpty();
            state.Name.Should().BeEmpty();
            state.Status.Should().Be(CustomerStatus.Active);
            state.Addresses.Should().BeEmpty();
            state.PaymentMethods.Should().BeEmpty();
        }

        [Fact]
        public void Apply_CustomerCreatedEvent_ShouldUpdateState()
        {
            // Arrange
            var state = new CustomerState();
            var customerEvent = new CustomerCreatedEvent("123", "test@example.com", "John Doe", DateTime.UtcNow);

            // Act
            var newState = state.Apply(customerEvent);

            // Assert
            newState.CustomerId.Should().Be("123");
            newState.Email.Should().Be("test@example.com");
            newState.Name.Should().Be("John Doe");
            newState.Status.Should().Be(CustomerStatus.Active);
        }

        [Fact]
        public void Apply_CustomerUpdatedEvent_ShouldUpdateNameAndEmail()
        {
            // Arrange
            var state = new CustomerState
            {
                CustomerId = "123",
                Email = "old@example.com",
                Name = "Old Name"
            };
            var updateEvent = new CustomerUpdatedEvent("123", "New Name", "new@example.com", DateTime.UtcNow);

            // Act
            var newState = state.Apply(updateEvent);

            // Assert
            newState.CustomerId.Should().Be("123");
            newState.Email.Should().Be("new@example.com");
            newState.Name.Should().Be("New Name");
        }

        [Fact]
        public void Apply_AddressAddedEvent_ShouldAddAddress()
        {
            // Arrange
            var state = new CustomerState { CustomerId = "123" };
            var address = new Address("123 Main St", "City", "State", "12345");
            var addressEvent = new AddressAddedEvent("123", address, DateTime.UtcNow);

            // Act
            var newState = state.Apply(addressEvent);

            // Assert
            newState.Addresses.Should().HaveCount(1);
            newState.Addresses[0].Should().Be(address);
        }

        [Fact]
        public void Apply_PaymentMethodAddedEvent_ShouldAddPaymentMethod()
        {
            // Arrange
            var state = new CustomerState { CustomerId = "123" };
            var paymentMethod = new PaymentMethod("CreditCard", "****1234", "12/25", "John Doe");
            var paymentEvent = new PaymentMethodAddedEvent("123", paymentMethod, DateTime.UtcNow);

            // Act
            var newState = state.Apply(paymentEvent);

            // Assert
            newState.PaymentMethods.Should().HaveCount(1);
            newState.PaymentMethods[0].Should().Be(paymentMethod);
        }

        [Fact]
        public void Apply_CustomerDeactivatedEvent_ShouldUpdateStatus()
        {
            // Arrange
            var state = new CustomerState 
            { 
                CustomerId = "123", 
                Status = CustomerStatus.Active 
            };
            var deactivateEvent = new CustomerDeactivatedEvent("123", "Account closed", DateTime.UtcNow);

            // Act
            var newState = state.Apply(deactivateEvent);

            // Assert
            newState.Status.Should().Be(CustomerStatus.Inactive);
        }
    }
}