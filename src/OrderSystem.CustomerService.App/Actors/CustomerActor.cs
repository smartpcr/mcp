// -----------------------------------------------------------------------
// <copyright file="CustomerActor.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.CustomerService.App.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Akka.Actor;
    using Akka.Event;
    using Akka.Persistence;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.Contracts.Models;
    using Address = OrderSystem.Contracts.Models.Address;

    public record CustomerState(
        string CustomerId = "",
        string Email = "",
        string Name = "",
        CustomerStatus Status = CustomerStatus.Active,
        ImmutableList<Address> Addresses = default,
        ImmutableList<PaymentMethod> PaymentMethods = default,
        DateTime CreatedAt = default,
        DateTime LastUpdated = default,
        ImmutableDictionary<string, object> Metadata = default)
    {
        public CustomerState() : this(
            CustomerId: "",
            Email: "",
            Name: "",
            Status: CustomerStatus.Active,
            Addresses: ImmutableList<Address>.Empty,
            PaymentMethods: ImmutableList<PaymentMethod>.Empty,
            CreatedAt: default,
            LastUpdated: default,
            Metadata: ImmutableDictionary<string, object>.Empty)
        {
        }

        public CustomerState Apply(ICustomerEvent evt) => evt switch
        {
            CustomerCreatedEvent e => this with
            {
                CustomerId = e.CustomerId,
                Email = e.Email,
                Name = e.Name,
                CreatedAt = e.CreatedAt,
                LastUpdated = e.CreatedAt
            },
            CustomerUpdatedEvent e => this with
            {
                Name = e.Name ?? this.Name,
                Email = e.Email ?? this.Email,
                LastUpdated = e.UpdatedAt
            },
            AddressAddedEvent e => this with
            {
                Addresses = this.Addresses.Add(e.Address),
                LastUpdated = e.AddedAt
            },
            PaymentMethodAddedEvent e => this with
            {
                PaymentMethods = this.PaymentMethods.Add(e.PaymentMethod),
                LastUpdated = e.AddedAt
            },
            CustomerDeactivatedEvent e => this with
            {
                Status = CustomerStatus.Inactive,
                LastUpdated = e.DeactivatedAt
            },
            _ => this
        };
    }

    public class CustomerActor : ReceivePersistentActor
    {
        private CustomerState state = new();
        private readonly string customerId;
        private readonly HashSet<string> processedCommands = new();
        private readonly ILoggingAdapter log;

        public CustomerActor(string customerId)
        {
            this.log = Context.GetLogger();
            this.customerId = customerId;

            this.SetupMessageHandlers();
            this.SetupEventSubscriptions();
        }

        public override string PersistenceId => $"customer-{this.customerId}";

        private void SetupMessageHandlers()
        {
            this.Command<CreateCustomer>(this.Handle);
            this.Command<UpdateCustomer>(this.Handle);
            this.Command<AddAddress>(this.Handle);
            this.Command<AddPaymentMethod>(this.Handle);
            this.Command<DeactivateCustomer>(this.Handle);
            this.Command<ValidateCustomer>(this.Handle);

            this.Recover<ICustomerEvent>(evt => this.state = this.state.Apply(evt));
            this.Recover<SnapshotOffer>(offer =>
            {
                if (offer.Snapshot is CustomerState state)
                {
                    this.state = state;
                }
            });
        }

        private void SetupEventSubscriptions()
        {
            // Subscribe to events from other bounded contexts that affect customers
            // For example, if orders affect customer status, payment methods, etc.
            // This enables choreography-based integration
        }

        private void Handle(CreateCustomer cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                this.Sender.Tell(new CustomerCreated(cmd.CustomerId));
                return;
            }

            if (!string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerAlreadyExists(this.customerId));
                return;
            }

            // Validate customer data
            if (!IsValidEmail(cmd.Email))
            {
                this.Sender.Tell(new CustomerValidationResult(this.customerId, false, "Invalid email format"));
                return;
            }

            var evt = new CustomerCreatedEvent(cmd.CustomerId, cmd.Email, cmd.Name, DateTime.UtcNow);

            this.Persist(evt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(new CustomerCreated(cmd.CustomerId));

                this.log.Info("Customer created: {0}", cmd.CustomerId);
                Context.System.EventStream.Publish(e);
            });
        }

        private void Handle(UpdateCustomer cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));
                return;
            }

            if (string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerNotFound(this.customerId));
                return;
            }

            // Validate email if provided
            if (!string.IsNullOrEmpty(cmd.Email) && !IsValidEmail(cmd.Email))
            {
                this.Sender.Tell(new CustomerValidationResult(this.customerId, false, "Invalid email format"));
                return;
            }

            var evt = new CustomerUpdatedEvent(cmd.CustomerId, cmd.Name, cmd.Email, DateTime.UtcNow);

            this.Persist(evt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));

                this.log.Info("Customer updated: {0}", cmd.CustomerId);
                Context.System.EventStream.Publish(e);
            });
        }

        private void Handle(AddAddress cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));
                return;
            }

            if (string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerNotFound(this.customerId));
                return;
            }

            // Validate address
            if (!IsValidAddress(cmd.Address))
            {
                this.Sender.Tell(new CustomerValidationResult(this.customerId, false, "Address is incomplete"));
                return;
            }

            var evt = new AddressAddedEvent(cmd.CustomerId, cmd.Address, DateTime.UtcNow);

            this.Persist(evt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));

                this.log.Info("Address added for customer: {0}", cmd.CustomerId);
                Context.System.EventStream.Publish(e);
            });
        }

        private void Handle(AddPaymentMethod cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));
                return;
            }

            if (string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerNotFound(this.customerId));
                return;
            }

            // Validate payment method
            if (!IsValidPaymentMethod(cmd.PaymentMethod))
            {
                this.Sender.Tell(new CustomerValidationResult(this.customerId, false, "Payment method type is required"));
                return;
            }

            var evt = new PaymentMethodAddedEvent(cmd.CustomerId, cmd.PaymentMethod, DateTime.UtcNow);

            this.Persist(evt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));

                this.log.Info("Payment method added for customer: {0}", cmd.CustomerId);
                Context.System.EventStream.Publish(e);
            });
        }

        private void Handle(DeactivateCustomer cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));
                return;
            }

            if (string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerNotFound(this.customerId));
                return;
            }

            if (this.state.Status == CustomerStatus.Inactive)
            {
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));
                return;
            }

            var evt = new CustomerDeactivatedEvent(cmd.CustomerId, cmd.Reason, DateTime.UtcNow);

            this.Persist(evt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));

                this.log.Info("Customer deactivated: {0}, Reason: {1}", cmd.CustomerId, cmd.Reason);
                Context.System.EventStream.Publish(e);
            });
        }

        private void Handle(ValidateCustomer cmd)
        {
            // This is a query, no need to check IsCommandProcessed or persist events
            if (string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerValidationResult(cmd.CustomerId, false, "Customer not found"));
                return;
            }

            var result = this.state.Status switch
            {
                CustomerStatus.Active => new CustomerValidationResult(this.customerId, true),
                CustomerStatus.Inactive => new CustomerValidationResult(this.customerId, false, "Customer is inactive"),
                CustomerStatus.Suspended => new CustomerValidationResult(this.customerId, false, "Customer is suspended"),
                _ => new CustomerValidationResult(this.customerId, false, "Unknown customer status")
            };

            this.log.Debug("Customer validation for {0}: {1}", this.customerId, result.IsValid);
            this.Sender.Tell(result);
        }

        // Static validation methods - no 'this' qualifier
        private static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) && email.Contains("@") && email.Contains(".");
        }

        private static bool IsValidAddress(Address address)
        {
            return !string.IsNullOrWhiteSpace(address.Street) &&
                   !string.IsNullOrWhiteSpace(address.City) &&
                   !string.IsNullOrWhiteSpace(address.ZipCode);
        }

        private static bool IsValidPaymentMethod(PaymentMethod paymentMethod)
        {
            return !string.IsNullOrWhiteSpace(paymentMethod.Type);
        }

        private bool IsCommandProcessed(string? correlationId)
        {
            if (string.IsNullOrEmpty(correlationId))
            {
                return false;
            }

            if (this.processedCommands.Contains(correlationId))
            {
                this.log.Debug("Command with correlation ID [{0}] already processed", correlationId);
                return true;
            }

            return false;
        }

        private void MarkCommandProcessed(string? correlationId)
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                this.processedCommands.Add(correlationId);
            }
        }

        protected override void PreStart()
        {
            this.log.Info("CustomerActor starting for customer: {0}", this.customerId);
            base.PreStart();
        }

        protected override void PostStop()
        {
            this.log.Info("CustomerActor stopping for customer: {0}", this.customerId);
            base.PostStop();
        }
    }
}