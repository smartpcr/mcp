using Shared.Contracts.Models;

namespace Shared.Contracts.Messages;

// Customer Commands
public record CreateCustomer(string CustomerId, string Email, string Name, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

public record UpdateCustomer(string CustomerId, string? Name = null, string? Email = null, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

public record AddAddress(string CustomerId, Address Address, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

public record AddPaymentMethod(string CustomerId, PaymentMethod PaymentMethod, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

public record DeactivateCustomer(string CustomerId, string Reason, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

public record ValidateCustomer(string CustomerId, string? CorrelationId = null) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}

// Customer Events
public interface ICustomerEvent : IEvent { string CustomerId { get; } }

public record CustomerCreatedEvent(string CustomerId, string Email, string Name, DateTime CreatedAt) : ICustomerEvent;
public record CustomerUpdatedEvent(string CustomerId, string? Name, string? Email, DateTime UpdatedAt) : ICustomerEvent;
public record AddressAddedEvent(string CustomerId, Address Address, DateTime AddedAt) : ICustomerEvent;
public record PaymentMethodAddedEvent(string CustomerId, PaymentMethod PaymentMethod, DateTime AddedAt) : ICustomerEvent;
public record CustomerDeactivatedEvent(string CustomerId, string Reason, DateTime DeactivatedAt) : ICustomerEvent;

// Customer Replies
public record CustomerCreated(string CustomerId);
public record CustomerUpdated(string CustomerId);
public record CustomerAlreadyExists(string CustomerId);
public record CustomerNotFound(string CustomerId);
public record CustomerValidationResult(string CustomerId, bool IsValid, string? Reason = null);