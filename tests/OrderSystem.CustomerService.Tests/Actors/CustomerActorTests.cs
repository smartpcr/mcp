// -----------------------------------------------------------------------
// <copyright file="CustomerActorTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.CustomerService.Tests.Actors
{
    using Akka.Actor;
    using Akka.TestKit.Xunit2;
    using FluentAssertions;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.Contracts.Models;
    using OrderSystem.CustomerService.App.Actors;
    using Xunit;
    using Address = OrderSystem.Contracts.Models.Address;

    public class CustomerActorTests : TestKit
    {
        [Fact]
        public void CustomerActor_CreateCustomer_ShouldReturnCustomerCreated()
        {
            // Arrange
            var customerId = "test-customer-123";
            var customerActor = this.ActorOf(Props.Create(() => new CustomerActor(customerId)));
            var createCommand = new CreateCustomer(customerId, "test@example.com", "John Doe");

            // Act
            customerActor.Tell(createCommand);

            // Assert
            var response = this.ExpectMsg<CustomerCreated>();
            response.CustomerId.Should().Be(customerId);
        }

        [Fact]
        public void CustomerActor_CreateCustomer_WhenAlreadyExists_ShouldReturnCustomerAlreadyExists()
        {
            // Arrange
            var customerId = "test-customer-123";
            var customerActor = this.ActorOf(Props.Create(() => new CustomerActor(customerId)));
            var createCommand = new CreateCustomer(customerId, "test@example.com", "John Doe");

            // Act - Create customer first time
            customerActor.Tell(createCommand);
            this.ExpectMsg<CustomerCreated>();

            // Act - Try to create same customer again
            customerActor.Tell(createCommand);

            // Assert
            var response = this.ExpectMsg<CustomerAlreadyExists>();
            response.CustomerId.Should().Be(customerId);
        }

        [Fact]
        public void CustomerActor_UpdateCustomer_WhenCustomerExists_ShouldReturnCustomerUpdated()
        {
            // Arrange
            var customerId = "test-customer-123";
            var customerActor = this.ActorOf(Props.Create(() => new CustomerActor(customerId)));

            // Create customer first
            var createCommand = new CreateCustomer(customerId, "test@example.com", "John Doe");
            customerActor.Tell(createCommand);
            this.ExpectMsg<CustomerCreated>();

            // Act - Update customer
            var updateCommand = new UpdateCustomer(customerId, "Jane Doe", "jane@example.com");
            customerActor.Tell(updateCommand);

            // Assert
            var response = this.ExpectMsg<CustomerUpdated>();
            response.CustomerId.Should().Be(customerId);
        }

        [Fact]
        public void CustomerActor_UpdateCustomer_WhenCustomerDoesNotExist_ShouldReturnCustomerNotFound()
        {
            // Arrange
            var customerId = "test-customer-123";
            var customerActor = this.ActorOf(Props.Create(() => new CustomerActor(customerId)));
            var updateCommand = new UpdateCustomer(customerId, "Jane Doe", "jane@example.com");

            // Act
            customerActor.Tell(updateCommand);

            // Assert
            var response = this.ExpectMsg<CustomerNotFound>();
            response.CustomerId.Should().Be(customerId);
        }

        [Fact]
        public void CustomerActor_ValidateCustomer_WhenActive_ShouldReturnValidResult()
        {
            // Arrange
            var customerId = "test-customer-123";
            var customerActor = this.ActorOf(Props.Create(() => new CustomerActor(customerId)));

            // Create customer first
            var createCommand = new CreateCustomer(customerId, "test@example.com", "John Doe");
            customerActor.Tell(createCommand);
            this.ExpectMsg<CustomerCreated>();

            // Act
            var validateCommand = new ValidateCustomer(customerId);
            customerActor.Tell(validateCommand);

            // Assert
            var response = this.ExpectMsg<CustomerValidationResult>();
            response.CustomerId.Should().Be(customerId);
            response.IsValid.Should().BeTrue();
            response.Reason.Should().BeNull();
        }

        [Fact]
        public void CustomerActor_ValidateCustomer_WhenNotExists_ShouldReturnInvalidResult()
        {
            // Arrange
            var customerId = "test-customer-123";
            var customerActor = this.ActorOf(Props.Create(() => new CustomerActor(customerId)));
            var validateCommand = new ValidateCustomer(customerId);

            // Act
            customerActor.Tell(validateCommand);

            // Assert
            var response = this.ExpectMsg<CustomerValidationResult>();
            response.CustomerId.Should().Be(customerId);
            response.IsValid.Should().BeFalse();
            response.Reason.Should().Be("Customer not found");
        }

        [Fact]
        public void CustomerActor_AddAddress_ShouldReturnCustomerUpdated()
        {
            // Arrange
            var customerId = "test-customer-123";
            var customerActor = this.ActorOf(Props.Create(() => new CustomerActor(customerId)));

            // Create customer first
            var createCommand = new CreateCustomer(customerId, "test@example.com", "John Doe");
            customerActor.Tell(createCommand);
            this.ExpectMsg<CustomerCreated>();

            // Act
            var address = new Address("123 Main St", "Anytown", "CA", "12345");
            var addAddressCommand = new AddAddress(customerId, address);
            customerActor.Tell(addAddressCommand);

            // Assert
            var response = this.ExpectMsg<CustomerUpdated>();
            response.CustomerId.Should().Be(customerId);
        }

        [Fact]
        public void CustomerActor_DeactivateCustomer_ShouldReturnCustomerUpdated()
        {
            // Arrange
            var customerId = "test-customer-123";
            var customerActor = this.ActorOf(Props.Create(() => new CustomerActor(customerId)));

            // Create customer first
            var createCommand = new CreateCustomer(customerId, "test@example.com", "John Doe");
            customerActor.Tell(createCommand);
            this.ExpectMsg<CustomerCreated>();

            // Act
            var deactivateCommand = new DeactivateCustomer(customerId, "Account closed by user");
            customerActor.Tell(deactivateCommand);

            // Assert
            var response = this.ExpectMsg<CustomerUpdated>();
            response.CustomerId.Should().Be(customerId);
        }
    }
}