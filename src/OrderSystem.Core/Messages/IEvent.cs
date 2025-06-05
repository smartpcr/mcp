// -----------------------------------------------------------------------
// <copyright file="IEvent.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Core.Messages
{
    public interface IEvent
    {
        string CorrelationId { get; }
        DateTime Timestamp { get; }
    }
}