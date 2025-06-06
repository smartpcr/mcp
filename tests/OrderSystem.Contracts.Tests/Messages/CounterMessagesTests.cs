// -----------------------------------------------------------------------
// <copyright file="CounterMessagesTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Tests.Messages
{
    using FluentAssertions;
    using OrderSystem.Contracts.Messages;
    using Xunit;

    public class CounterMessagesTests
    {
        [Fact]
        public void IncrementCounterCommand_ShouldCreateWithCounterId()
        {
            // Arrange & Act
            var command = new IncrementCounterCommand("counter-123", 1);

            // Assert
            command.CounterId.Should().Be("counter-123");
            command.Amount.Should().Be(1);
            command.Should().BeAssignableTo<ICounterCommand>();
            command.Should().BeAssignableTo<IWithCounterId>();
        }

        [Fact]
        public void DecrementCounterCommand_ShouldCreateWithCounterId()
        {
            // Arrange & Act
            var command = new DecrementCounterCommand("counter-456");

            // Assert
            command.CounterId.Should().Be("counter-456");
            command.Should().BeAssignableTo<ICounterCommand>();
            command.Should().BeAssignableTo<IWithCounterId>();
        }

        [Fact]
        public void SetCounterCommand_ShouldCreateWithCounterIdAndValue()
        {
            // Arrange & Act
            var command = new SetCounterCommand("counter-789", 42);

            // Assert
            command.CounterId.Should().Be("counter-789");
            command.Value.Should().Be(42);
            command.Should().BeAssignableTo<ICounterCommand>();
            command.Should().BeAssignableTo<IWithCounterId>();
        }

        [Fact]
        public void CounterIncrementedEvent_ShouldCreateWithCounterIdAndValue()
        {
            // Arrange & Act
            var evt = new CounterIncrementedEvent("counter-123", 5);

            // Assert
            evt.CounterId.Should().Be("counter-123");
            evt.NewValue.Should().Be(5);
            evt.Should().BeAssignableTo<ICounterEvent>();
            evt.Should().BeAssignableTo<IWithCounterId>();
        }

        [Fact]
        public void CounterDecrementedEvent_ShouldCreateWithCounterIdAndValue()
        {
            // Arrange & Act
            var evt = new CounterDecrementedEvent("counter-456", 3);

            // Assert
            evt.CounterId.Should().Be("counter-456");
            evt.NewValue.Should().Be(3);
            evt.Should().BeAssignableTo<ICounterEvent>();
            evt.Should().BeAssignableTo<IWithCounterId>();
        }

        [Fact]
        public void CounterSetEvent_ShouldCreateWithCounterIdAndValue()
        {
            // Arrange & Act
            var evt = new CounterSetEvent("counter-789", 100);

            // Assert
            evt.CounterId.Should().Be("counter-789");
            evt.NewValue.Should().Be(100);
            evt.Should().BeAssignableTo<ICounterEvent>();
            evt.Should().BeAssignableTo<IWithCounterId>();
        }

        [Fact]
        public void FetchCounter_ShouldCreateWithCounterId()
        {
            // Arrange & Act
            var query = new FetchCounter("counter-999");

            // Assert
            query.CounterId.Should().Be("counter-999");
            query.Should().BeAssignableTo<IWithCounterId>();
        }

        [Fact]
        public void CounterCommandResponse_ShouldCreateWithCounterIdAndSuccess()
        {
            // Arrange & Act
            var response = new CounterCommandResponse("counter-123", true);

            // Assert
            response.CounterId.Should().Be("counter-123");
            response.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void CounterCommandResponse_ShouldCreateWithFailure()
        {
            // Arrange & Act
            var response = new CounterCommandResponse("counter-456", false);

            // Assert
            response.CounterId.Should().Be("counter-456");
            response.IsSuccess.Should().BeFalse();
        }
    }
}