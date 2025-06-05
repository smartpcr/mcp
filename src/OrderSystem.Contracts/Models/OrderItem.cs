// -----------------------------------------------------------------------
// <copyright file="OrderItem.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Shared.Contracts.Models
{
    public record OrderItem(
        string ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice)
    {
        public decimal TotalPrice => this.Quantity * this.UnitPrice;
    }
}