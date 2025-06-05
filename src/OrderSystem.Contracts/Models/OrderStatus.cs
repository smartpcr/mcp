// -----------------------------------------------------------------------
// <copyright file="OrderStatus.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Shared.Contracts.Models
{
    public enum OrderStatus
    {
        Pending,
        PaymentProcessing,
        PaymentConfirmed,
        Preparing,
        Shipped,
        Delivered,
        Cancelled,
        PaymentFailed,
        OutOfStock,
        ShipmentFailed,
        ReturnRequested
    }
}