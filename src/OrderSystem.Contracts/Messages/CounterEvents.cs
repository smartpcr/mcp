// -----------------------------------------------------------------------
// <copyright file="CounterEvents.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Messages
{
    /// <summary>
    /// Events are facts of the system. Counter events deal in definitive state changes with the counter.
    /// </summary>
    public interface ICounterEvent : IWithCounterId
    {
    }

    public sealed record CounterIncrementedEvent(string CounterId, int NewValue) : ICounterEvent;

    public sealed record CounterDecrementedEvent(string CounterId, int NewValue) : ICounterEvent;

    public sealed record CounterSetEvent(string CounterId, int NewValue) : ICounterEvent;
}