// -----------------------------------------------------------------------
// <copyright file="IWithCounterId.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Messages
{
    /// <summary>
    /// Counters are the only entities that have a counter id.
    ///
    /// All messages decorated with this interface belong to a specific counter.
    /// </summary>
    public interface IWithCounterId
    {
        string CounterId { get; }
    }
}