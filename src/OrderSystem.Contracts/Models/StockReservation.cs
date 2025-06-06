// -----------------------------------------------------------------------
// <copyright file="StockReservation.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Models
{
    using System;

    public record StockReservation(string OrderId, int Quantity, DateTime ReservedAt);
}