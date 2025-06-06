// -----------------------------------------------------------------------
// <copyright file="CounterActorTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.ShipmentService.Tests.Actors
{
    using Akka.Actor;
    using Akka.TestKit.Xunit2;
    using FluentAssertions;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.ShipmentService.App.Actors;
    using Xunit;

    public class CounterActorTests : TestKit
    {
        [Fact]
        public void CounterActor_FetchCounter_ShouldReturnInitialCounter()
        {
            // Arrange
            var counterName = "shipment-counter";
            var counterActor = this.ActorOf(Props.Create(() => new CounterActor(counterName)));

            // Act
            counterActor.Tell(new FetchCounter(counterName));

            // Assert
            var response = this.ExpectMsg<OrderSystem.Contracts.Messages.Counter>();
            response.CounterId.Should().Be(counterName);
            response.CurrentValue.Should().Be(0);
        }

        [Fact]
        public void CounterActor_IncrementCommand_ShouldWork()
        {
            // Arrange
            var counterName = "shipment-counter";
            var counterActor = this.ActorOf(Props.Create(() => new CounterActor(counterName)));

            // Act
            counterActor.Tell(new IncrementCounterCommand(counterName, 1));

            // Assert
            var response = this.ExpectMsg<CounterCommandResponse>();
            response.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void CounterActor_SetCommand_ShouldWork()
        {
            // Arrange
            var counterName = "shipment-counter";
            var counterActor = this.ActorOf(Props.Create(() => new CounterActor(counterName)));

            // Act
            counterActor.Tell(new SetCounterCommand(counterName, 25));

            // Assert
            var response = this.ExpectMsg<CounterCommandResponse>();
            response.IsSuccess.Should().BeTrue();
        }
    }
}