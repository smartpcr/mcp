﻿// -----------------------------------------------------------------------
// <copyright file="CounterActor.cs" company="The NBS Project">
// Copyright (c) The NBS Project. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.ShipmentService.App.Actors
{
    using System;
    using System.Collections.Generic;
    using Akka.Actor;
    using Akka.Event;
    using Akka.Persistence;
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
                    new CounterIncrementedEvent(counter.CounterId,
                        increment.Amount + counter.CurrentValue)),
                SetCounterCommand set => new CounterCommandResponse(counter.CounterId, true,
                    new CounterSetEvent(counter.CounterId, set.Value)),
                _ => throw new InvalidOperationException($"Unknown command type: {command.GetType().Name}")
            };
        }

        public static Counter ApplyEvent(this Counter counter, ICounterEvent @event)
        {
            return @event switch
            {
                CounterIncrementedEvent increment => counter with { CurrentValue = increment.NewValue },
                CounterSetEvent set => counter with { CurrentValue = set.NewValue },
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
            PersistenceId = $"Counter_{counterName}";
            this.counter = new Counter(counterName, 0);


            Recover<SnapshotOffer>(offer =>
            {
                if (offer.Snapshot is Counter c)
                {
                    this.counter = c;
                    this.log.Info("Recovered initial count value of [{0}]", c);
                }
            });

            Recover<ICounterEvent>(@event =>
            {
                this.counter = this.counter.ApplyEvent(@event);
            });

            Command<FetchCounter>(f => Sender.Tell(this.counter));

            Command<SubscribeToCounter>(subscribe =>
            {
                this.subscribers.Add(subscribe.Subscriber);
                Sender.Tell(new CounterCommandResponse(this.counter.CounterId, true));
                Context.Watch(subscribe.Subscriber);
            });

            Command<UnsubscribeToCounter>(counter =>
            {
                Context.Unwatch(counter.Subscriber);
                this.subscribers.Remove(counter.Subscriber);
            });

            Command<ICounterCommand>(cmd =>
            {
                var response = this.counter.ProcessCommand(cmd);

                if (!response.IsSuccess)
                {
                    Sender.Tell(response);
                    return;
                }

                if (response.Event != null) // only persist if there is an event to persist
                {
                    Persist(response.Event, @event =>
                    {
                        this.counter = this.counter.ApplyEvent(@event);
                        this.log.Info("Updated counter via {0} - new value is {1}", @event, this.counter.CurrentValue);
                        Sender.Tell(response);

                        // push events to all subscribers
                        foreach (var s in this.subscribers)
                        {
                            s.Tell(@event);
                        }
                        SaveSnapshotWhenAble();
                    });
                }
            });

            Command<SaveSnapshotSuccess>(success =>
            {
                // delete all older snapshots (but leave journal intact, in case we want to do projections with that data)
                DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));
            });
        }

        private void SaveSnapshotWhenAble()
        {
            // save a new snapshot every 25 events, in order to keep recovery times bounded
            if (LastSequenceNr % 25 == 0)
            {
                SaveSnapshot(this.counter);
            }
        }

        public override string PersistenceId { get; }
    }
}