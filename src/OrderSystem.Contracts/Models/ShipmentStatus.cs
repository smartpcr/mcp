// -----------------------------------------------------------------------
// <copyright file="ShipmentStatus.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Models
{
    public enum ShipmentStatus
    {
        Pending,
        Created,
        Scheduled,
        InTransit,
        Delivered,
        Failed
    }
}