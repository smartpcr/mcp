// -----------------------------------------------------------------------
// <copyright file="CounterActorConfiguration.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Infrastructure.Configuration
{
    using System;
    using Akka.Actor;
    using Akka.Cluster.Sharding;
    using Akka.Cluster.Hosting;
    using Akka.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.Infrastructure.Actors;

    public static class CounterActorConfiguration
    {
        public static AkkaConfigurationBuilder ConfigureCounterActors<TCounterActor>(this AkkaConfigurationBuilder builder,
            IServiceProvider serviceProvider)
            where TCounterActor : ActorBase
        {
            var settings = serviceProvider.GetRequiredService<AkkaSettings>();
            var extractor = CreateCounterMessageRouter();

            if (settings.UseClustering)
            {
                return builder.WithShardRegion<TCounterActor>("counter",
                    (system, registry, resolver) => s => Props.Create(typeof(TCounterActor), s),
                    extractor, settings.ShardOptions);
            }

            return builder.WithActors((system, registry, resolver) =>
            {
                var parent = system.ActorOf(
                    GenericChildPerEntityParent.Props(extractor, s => Props.Create(typeof(TCounterActor), s)),
                    "counters");
                registry.Register<TCounterActor>(parent);
            });
        }

        public static HashCodeMessageExtractor CreateCounterMessageRouter()
        {
            return HashCodeMessageExtractor.Create(30, o =>
            {
                return o switch
                {
                    IWithCounterId counterId => counterId.CounterId,
                    _ => null
                };
            }, o => o);
        }
    }
}