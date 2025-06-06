// -----------------------------------------------------------------------
// <copyright file="CounterQueries.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Messages
{
    /// <summary>
    /// Queries are similar to commands, but they have no side effects.
    ///
    /// They are used to retrieve information from the actors.
    /// </summary>
    public interface ICounterQuery : IWithCounterId
    {
    }

    public sealed record FetchCounter(string CounterId) : ICounterQuery;

    /// <summary>
    /// Represents the current state of a counter.
    /// </summary>
    public sealed record Counter(string CounterId, int CurrentValue);
}