// -----------------------------------------------------------------------
// <copyright file="PaymentAttempt.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Models
{
    using System;

    public record PaymentAttempt(DateTime AttemptedAt, bool Success, string? Response = null);
}