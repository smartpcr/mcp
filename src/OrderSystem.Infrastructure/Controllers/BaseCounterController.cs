// -----------------------------------------------------------------------
// <copyright file="BaseCounterController.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Infrastructure.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Akka.Actor;
    using Akka.Hosting;
    using Microsoft.Extensions.Logging;
    using OrderSystem.Contracts.Messages;

    public abstract class BaseCounterController<TCounterActor, TCounter>
        where TCounterActor : ActorBase
    {
        protected readonly ILogger Logger;
        protected readonly IActorRef CounterActor;

        protected BaseCounterController(ILogger logger, IRequiredActor<TCounterActor> counterActor)
        {
            this.Logger = logger;
            this.CounterActor = counterActor.ActorRef;
        }

        public async Task<TCounter> GetCounter(string counterId)
        {
            var counter = await this.CounterActor.Ask<TCounter>(new FetchCounter(counterId), TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            return counter;
        }

        public async Task<CounterCommandResponse> IncrementCounter(string counterId, int increment)
        {
            var result = await this.CounterActor.Ask<CounterCommandResponse>(new IncrementCounterCommand(counterId, increment), TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            return result;
        }

        public async Task<CounterCommandResponse> SetCounter(string counterId, int counterValue)
        {
            var result = await this.CounterActor.Ask<CounterCommandResponse>(new SetCounterCommand(counterId, counterValue), TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            return result;
        }
    }
}