// -----------------------------------------------------------------------
// <copyright file="PaymentMethod.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Models
{
    public record PaymentMethod(
        string Type,
        string? CardNumber = null,
        string? ExpiryDate = null,
        string? CardHolderName = null,
        string? BankAccountNumber = null
    );
}