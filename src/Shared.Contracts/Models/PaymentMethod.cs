// -----------------------------------------------------------------------
// <copyright file="PaymentMethod.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Shared.Contracts.Models
{
    using System;

    public record PaymentMethod(
        string Type,
        string? CardNumber = null,
        string? ExpiryDate = null,
        string? CardHolderName = null,
        string? BankAccountNumber = null);

    public enum PaymentStatus
    {
        Pending,
        Processing,
        Succeeded,
        Failed,
        Refunded,
        Cancelled
    }

    public record PaymentAttempt(DateTime AttemptedAt, bool Success, string? Response = null);

    public record StockReservation(string OrderId, int Quantity, DateTime ReservedAt);

    public enum ProductStatus
    {
        Active,
        Inactive,
        OutOfStock
    }

    public enum CustomerStatus
    {
        Active,
        Inactive,
        Suspended
    }

    public enum ShipmentStatus
    {
        Pending,
        Created,
        Scheduled,
        InTransit,
        Delivered,
        Failed
    }

    public record ShipmentUpdate(DateTime UpdatedAt, string Status, string? Location = null);
}