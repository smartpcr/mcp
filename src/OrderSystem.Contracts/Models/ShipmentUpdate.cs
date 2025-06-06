// -----------------------------------------------------------------------
// <copyright file="ShipmentUpdate.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Models
{
    using System;

    public record ShipmentUpdate(DateTime UpdatedAt, string Status, string? Location = null);
}