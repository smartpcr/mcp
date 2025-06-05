// -----------------------------------------------------------------------
// <copyright file="CheckAvailability.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Shared.Contracts.Messages
{
    using System;
    using OrderSystem.Contracts.Messages;

    public record CheckAvailability(
        string ProductId,
        int Quantity,
        string CorrelationId = null!) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }
}