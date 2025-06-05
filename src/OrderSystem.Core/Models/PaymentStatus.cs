// -----------------------------------------------------------------------
// <copyright file="PaymentStatus.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Core.Models
{
    public enum PaymentStatus
    {
        Pending,
        Processing,
        Succeeded,
        Failed,
        Refunded
    }
}