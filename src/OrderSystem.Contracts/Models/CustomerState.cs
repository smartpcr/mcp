
namespace OrderSystem.Contracts.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OrderSystem.Contracts.Messages;

    public record CustomerState
    {
        public string CustomerId { get; init; } = "";
        public string Email { get; init; } = "";
        public string Name { get; init; } = "";
        public CustomerStatus Status { get; init; } = CustomerStatus.Active;
        public List<Address> Addresses { get; init; } = new();
        public List<PaymentMethod> PaymentMethods { get; init; } = new();
        public DateTime CreatedAt { get; init; }
        public DateTime LastUpdated { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();

        public CustomerState Apply(ICustomerEvent evt) => evt switch
        {
            CustomerCreatedEvent e => this with
            {
                CustomerId = e.CustomerId,
                Email = e.Email,
                Name = e.Name,
                Status = CustomerStatus.Active, // Explicitly set active on creation
                CreatedAt = e.CreatedAt,
                LastUpdated = e.CreatedAt
            },
            CustomerUpdatedEvent e => this with
            {
                Name = e.Name ?? Name,
                Email = e.Email ?? Email,
                LastUpdated = e.UpdatedAt
            },
            AddressAddedEvent e => this with
            {
                Addresses = Addresses.Append(e.Address).ToList(),
                LastUpdated = e.AddedAt
            },
            PaymentMethodAddedEvent e => this with
            {
                PaymentMethods = PaymentMethods.Append(e.PaymentMethod).ToList(),
                LastUpdated = e.AddedAt
            },
            CustomerDeactivatedEvent e => this with
            {
                Status = CustomerStatus.Inactive,
                LastUpdated = e.DeactivatedAt
            },
            _ => this // Default case to handle unknown events
        };
    }
}
