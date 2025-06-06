// -----------------------------------------------------------------------
// <copyright file="CounterActorTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.CustomerService.Tests.Actors
{
    using Akka.Actor;
    using Akka.TestKit.Xunit2;
    using FluentAssertions;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.CustomerService.App.Actors;
    using Xunit;

    public class CounterActorTests : TestKit
    {
        [Fact]
        public void CounterActor_FetchCounter_ShouldReturnInitialCounter()
        {
            // Arrange
            var counterName = "test-counter";
            var counterActor = this.ActorOf(Props.Create(() => new CounterActor(counterName)));

            // Act
            counterActor.Tell(new FetchCounter(counterName));

            // Assert
            var response = this.ExpectMsg<OrderSystem.Contracts.Messages.Counter>();
            response.CounterId.Should().Be(counterName);
            response.CurrentValue.Should().Be(0);
        }

        [Fact]
        public void CounterActor_IncrementCommand_ShouldIncrementCounter()
        {
            // Arrange
            var counterName = "test-counter";
            var counterActor = this.ActorOf(Props.Create(() => new CounterActor(counterName)));

            // Act
            counterActor.Tell(new IncrementCounterCommand(counterName, 1));

            // Assert
            var response = this.ExpectMsg<CounterCommandResponse>();
            response.CounterId.Should().Be(counterName);
            response.IsSuccess.Should().BeTrue();

            // Verify counter value
            counterActor.Tell(new FetchCounter(counterName));
            var counter = this.ExpectMsg<OrderSystem.Contracts.Messages.Counter>();
            counter.CurrentValue.Should().Be(1);
        }

        [Fact]
        public void CounterActor_DecrementCommand_ShouldDecrementCounter()
        {
            // Arrange
            var counterName = "test-counter";
            var counterActor = this.ActorOf(Props.Create(() => new CounterActor(counterName)));

            // First increment to have a positive value
            counterActor.Tell(new IncrementCounterCommand(counterName, 1));
            this.ExpectMsg<CounterCommandResponse>();

            // Act
            counterActor.Tell(new DecrementCounterCommand(counterName));

            // Assert
            var response = this.ExpectMsg<CounterCommandResponse>();
            response.CounterId.Should().Be(counterName);
            response.IsSuccess.Should().BeTrue();

            // Verify counter value
            counterActor.Tell(new FetchCounter(counterName));
            var counter = this.ExpectMsg<OrderSystem.Contracts.Messages.Counter>();
            counter.CurrentValue.Should().Be(0);
        }

        [Fact]
        public void CounterActor_SetCommand_ShouldSetCounterValue()
        {
            // Arrange
            var counterName = "test-counter";
            var counterActor = this.ActorOf(Props.Create(() => new CounterActor(counterName)));

            // Act
            counterActor.Tell(new SetCounterCommand(counterName, 42));

            // Assert
            var response = this.ExpectMsg<CounterCommandResponse>();
            response.CounterId.Should().Be(counterName);
            response.IsSuccess.Should().BeTrue();

            // Verify counter value
            counterActor.Tell(new FetchCounter(counterName));
            var counter = this.ExpectMsg<OrderSystem.Contracts.Messages.Counter>();
            counter.CurrentValue.Should().Be(42);
        }

        [Fact]
        public void CounterActor_SubscribeToCounter_ShouldAddSubscriber()
        {
            // Arrange
            var counterName = "test-counter";
            var counterActor = this.ActorOf(Props.Create(() => new CounterActor(counterName)));
            var subscriber = this.CreateTestProbe();

            // Act
            counterActor.Tell(new SubscribeToCounter(counterName, subscriber));

            // Assert
            var response = this.ExpectMsg<CounterCommandResponse>();
            response.CounterId.Should().Be(counterName);
            response.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void CounterActor_IncrementWithSubscriber_ShouldNotifySubscriber()
        {
            // Arrange
            var counterName = "test-counter";
            var counterActor = this.ActorOf(Props.Create(() => new CounterActor(counterName)));
            var subscriber = this.CreateTestProbe();

            // Subscribe first
            counterActor.Tell(new SubscribeToCounter(counterName, subscriber));
            this.ExpectMsg<CounterCommandResponse>();

            // Act
            counterActor.Tell(new IncrementCounterCommand(counterName, 1));

            // Assert
            this.ExpectMsg<CounterCommandResponse>();
            subscriber.ExpectMsg<CounterIncrementedEvent>();
        }

        [Fact]
        public void CounterActor_UnsubscribeFromCounter_ShouldRemoveSubscriber()
        {
            // Arrange
            var counterName = "test-counter";
            var counterActor = this.ActorOf(Props.Create(() => new CounterActor(counterName)));
            var subscriber = this.CreateTestProbe();

            // Subscribe first
            counterActor.Tell(new SubscribeToCounter(counterName, subscriber));
            this.ExpectMsg<CounterCommandResponse>();

            // Act
            counterActor.Tell(new UnsubscribeToCounter(counterName, subscriber));

            // Increment after unsubscribe
            counterActor.Tell(new IncrementCounterCommand(counterName, 1));
            this.ExpectMsg<CounterCommandResponse>();

            // Assert - subscriber should not receive notification
            subscriber.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public void CounterActor_MultipleIncrements_ShouldAccumulate()
        {
            // Arrange
            var counterName = "test-counter";
            var counterActor = this.ActorOf(Props.Create(() => new CounterActor(counterName)));

            // Act
            for (var i = 0; i < 5; i++)
            {
                counterActor.Tell(new IncrementCounterCommand(counterName, 1));
                this.ExpectMsg<CounterCommandResponse>();
            }

            // Assert
            counterActor.Tell(new FetchCounter(counterName));
            var counter = this.ExpectMsg<OrderSystem.Contracts.Messages.Counter>();
            counter.CurrentValue.Should().Be(5);
        }
    }
}