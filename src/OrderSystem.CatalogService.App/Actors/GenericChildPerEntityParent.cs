// -----------------------------------------------------------------------
// <copyright file="GenericChildPerEntityParent.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.CatalogService.App.Actors
{
    using System;
    using Akka.Actor;
    using Akka.Cluster.Sharding;

    /// <summary>
    /// A generic "child per entity" parent actor.
    /// </summary>
    /// <remarks>
    /// Intended for simplifying unit tests where we don't want to use Akka.Cluster.Sharding.
    /// </remarks>
    public sealed class GenericChildPerEntityParent : ReceiveActor
    {
        public static Props Props(IMessageExtractor extractor, Func<string, Props> propsFactory)
        {
            return Akka.Actor.Props.Create(() => new GenericChildPerEntityParent(extractor, propsFactory));
        }

        /*
         * Re-use Akka.Cluster.Sharding's infrastructure here to keep things simple.
         */
        private readonly IMessageExtractor extractor;
        private readonly Func<string, Props> propsFactory;

        public GenericChildPerEntityParent(IMessageExtractor extractor, Func<string, Props> propsFactory)
        {
            this.extractor = extractor;
            this.propsFactory = propsFactory;

            this.ReceiveAny(o =>
            {
                var entityId = this.extractor.EntityId(o);
                if (string.IsNullOrEmpty(entityId))
                    return;
                Context.Child(entityId).GetOrElse(() => Context.ActorOf(this.propsFactory(entityId), entityId))
                    .Forward(this.extractor.EntityMessage(o));
            });
        }
    }
}