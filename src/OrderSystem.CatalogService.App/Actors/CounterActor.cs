// -----------------------------------------------------------------------
// <copyright file="CounterActor.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.CatalogService.App.Actors
{
    using System;
    using System.Collections.Generic;
    using Akka.Actor;
    using Akka.Event;
    using Akka.Persistence;
    using OrderSystem.CatalogService.Domain;
    using OrderSystem.Contracts.Messages;

    public record Counter(string CounterId, int CurrentValue)
    {
    }

    public static class CounterExtensions
    {
        public static CounterCommandResponse ProcessCommand(this Counter counter, ICounterCommand command)
        {
            return command switch
            {
                IncrementCounterCommand increment => new CounterCommandResponse(counter.CounterId, true,
                    new CounterValueIncremented(counter.CounterId, increment.Amount,
                        increment.Amount + counter.CurrentValue)),
                SetCounterCommand set => new CounterCommandResponse(counter.CounterId, true,
                    new CounterValueSet(counter.CounterId, set.Value)),
                _ => throw new InvalidOperationException($"Unknown command type: {command.GetType().Name}")
            };
        }

        public static Counter ApplyEvent(this Counter counter, ICounterEvent @event)
        {
            return @event switch
            {
                CounterValueIncremented increment => counter with {CurrentValue = increment.NewValue},
                CounterValueSet set => counter with {CurrentValue = set.NewValue},
                _ => throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}")
            };
        }
    }

    public sealed class CounterActor : ReceivePersistentActor
    {
        // currently, do not persist subscribers, but would be easy to add
        private readonly HashSet<IActorRef> subscribers = new();
        private Counter counter;
        private readonly ILoggingAdapter log = Context.GetLogger();

        public CounterActor(string counterName)
        {
            // distinguish both type and entity Id in the EventJournal
            this.PersistenceId = $"Counter_{counterName}";
            this.counter = new Counter(counterName, 0);


            this.Recover<SnapshotOffer>(offer =>
            {
                if (offer.Snapshot is Counter c)
                {
                    this.counter = c;
                    this.log.Info("Recovered initial count value of [{0}]", c);
                }
            });

            this.Recover<ICounterEvent>(@event =>
            {
                this.counter = this.counter.ApplyEvent(@event);
            });

            this.Command<FetchCounter>(f => this.Sender.Tell(this.counter));

            this.Command<SubscribeToCounter>(subscribe =>
            {
                this.subscribers.Add(subscribe.Subscriber);
                this.Sender.Tell(new CounterCommandResponse(this.counter.CounterId, true));
                UntypedPersistentActor.Context.Watch(subscribe.Subscriber);
            });

            this.Command<UnsubscribeToCounter>(counter =>
            {
                UntypedPersistentActor.Context.Unwatch(counter.Subscriber);
                this.subscribers.Remove(counter.Subscriber);
            });

            this.Command<ICounterCommand>(cmd =>
            {
                var response = this.counter.ProcessCommand(cmd);

                if (!response.IsSuccess)
                {
                    this.Sender.Tell(response);
                    return;
                }

                if (response.Event != null) // only persist if there is an event to persist
                {
                    this.Persist(response.Event, @event =>
                    {
                        this.counter = this.counter.ApplyEvent(@event);
                        this.log.Info("Updated counter via {0} - new value is {1}", @event, this.counter.CurrentValue);
                        this.Sender.Tell(response);

                        // push events to all subscribers
                        foreach (var s in this.subscribers)
                        {
                            s.Tell(@event);
                        }
                        this.SaveSnapshotWhenAble();
                    });
                }
            });

            this.Command<SaveSnapshotSuccess>(success =>
            {
                // delete all older snapshots (but leave journal intact, in case we want to do projections with that data)
                this.DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));
            });
        }

        private void SaveSnapshotWhenAble()
        {
            // save a new snapshot every 25 events, in order to keep recovery times bounded
            if (this.LastSequenceNr % 25 == 0)
            {
                this.SaveSnapshot(this.counter);
            }
        }

        public override string PersistenceId { get; }
    }
}