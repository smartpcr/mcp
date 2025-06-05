// -----------------------------------------------------------------------
// <copyright file="Address.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Core.Models
{
    public record Address(
        string Street,
        string City,
        string State,
        string ZipCode,
        string Country = "US");
}