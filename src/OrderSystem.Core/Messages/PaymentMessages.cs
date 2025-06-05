// -----------------------------------------------------------------------
// <copyright file="PaymentMessages.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Core.Messages
{
    using OrderSystem.Core.Models;

    // Commands
    public record ProcessPayment(
        string OrderId,
        string AccountId,
        decimal Amount,
        string PaymentMethod,
        string CorrelationId = null!) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }

    public record RefundPayment(
        string PaymentId,
        decimal Amount,
        string CorrelationId = null!) : ICommand
    {
        public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
    }

    // Events
    public record PaymentSucceeded(
        string PaymentId,
        string OrderId,
        string TransactionId,
        decimal Amount,
        string CorrelationId,
        DateTime Timestamp = default) : IEvent
    {
        public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
    }

    public record PaymentFailed(
        string PaymentId,
        string OrderId,
        string Reason,
        string CorrelationId,
        DateTime Timestamp = default) : IEvent
    {
        public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
    }

    public record PaymentRefunded(
        string PaymentId,
        decimal Amount,
        string CorrelationId,
        DateTime Timestamp = default) : IEvent
    {
        public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
    }
}