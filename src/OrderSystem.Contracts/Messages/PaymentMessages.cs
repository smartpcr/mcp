using Shared.Contracts.Models;

namespace Shared.Contracts.Messages;

// Payment Commands
public record ProcessPayment(string PaymentId, string OrderId, string CustomerId, decimal Amount, PaymentMethod Method, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

public record RefundPayment(string PaymentId, decimal? Amount = null, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

public record RetryPayment(string PaymentId, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

// Payment Events  
public interface IPaymentEvent : IEvent { string PaymentId { get; } }

public record PaymentInitiatedEvent(string PaymentId, string OrderId, string CustomerId, decimal Amount, PaymentMethod Method, DateTime InitiatedAt) : IPaymentEvent;
public record PaymentSucceededEvent(string PaymentId, string OrderId, string TransactionId, string GatewayResponse, DateTime ProcessedAt) : IPaymentEvent;
public record PaymentFailedEvent(string PaymentId, string OrderId, string Reason, DateTime ProcessedAt) : IPaymentEvent;
public record PaymentRefundedEvent(string PaymentId, decimal Amount, DateTime RefundedAt) : IPaymentEvent;

// Payment Replies
public record PaymentResult(string PaymentId, bool Success, string? TransactionId = null, string? Reason = null);
public record RefundResult(string PaymentId, bool Success, string? Reason = null);

// External service interfaces
public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(string paymentId, decimal amount, PaymentMethod method);
}

public record PaymentGatewayResult(bool Success, string? TransactionId = null, string? Reason = null, string? Response = null);