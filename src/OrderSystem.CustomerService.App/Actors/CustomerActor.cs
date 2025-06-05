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

        public CustomerActor(string customerId)
        {
            this.customerId = customerId;

            this.Command<CreateCustomer>(this.Handle);
            this.Command<UpdateCustomer>(this.Handle);
            this.Command<AddAddress>(this.Handle);
            this.Command<AddPaymentMethod>(this.Handle);
            this.Command<DeactivateCustomer>(this.Handle);
            this.Command<ValidateCustomer>(this.Handle);

            this.Recover<ICustomerEvent>(evt => this.state = this.state.Apply(evt));
            this.Recover<RecoveryCompleted>(_ => {
                Console.WriteLine($"Customer {this.customerId} recovery completed. Status: {this.state.Status}");
            });
        }

        public override string PersistenceId => $"customer-{this.customerId}";

        private void Handle(CreateCustomer cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                this.Sender.Tell(new CustomerCreated(this.customerId));
                return;
            }

            if (!string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerAlreadyExists(this.customerId));
                return;
            }

            // Validate email format
            if (string.IsNullOrWhiteSpace(cmd.Email) || !cmd.Email.Contains("@"))
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

                Console.WriteLine($"Customer created: {cmd.CustomerId}");
                UntypedPersistentActor.Context.System.EventStream.Publish(e);
            });
        }

        private void Handle(ValidateCustomer cmd)
        {
            if (string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerValidationResult(this.customerId, false, "Customer not found"));
                return;
            }

            var result = this.state.Status switch
            {
                CustomerStatus.Active => new CustomerValidationResult(this.customerId, true),
                CustomerStatus.Inactive => new CustomerValidationResult(this.customerId, false, "Customer is inactive"),
                CustomerStatus.Suspended => new CustomerValidationResult(this.customerId, false, "Customer is suspended"),
                _ => new CustomerValidationResult(this.customerId, false, "Unknown customer status")
            };

            Console.WriteLine($"Customer validation for {this.customerId}: {result.IsValid}");
            this.Sender.Tell(result);
        }

        private void Handle(UpdateCustomer cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                this.Sender.Tell(new CustomerUpdated(this.customerId));
                return;
            }

            if (string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerNotFound(this.customerId));
                return;
            }

            // Validate email if provided
            if (!string.IsNullOrEmpty(cmd.Email) && !cmd.Email.Contains("@"))
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

                Console.WriteLine($"Customer updated: {cmd.CustomerId}");
                UntypedPersistentActor.Context.System.EventStream.Publish(e);
            });
        }

        private void Handle(AddAddress cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                this.Sender.Tell(new CustomerUpdated(this.customerId));
                return;
            }

            if (string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerNotFound(this.customerId));
                return;
            }

            // Validate address
            if (string.IsNullOrWhiteSpace(cmd.Address.Street) ||
                string.IsNullOrWhiteSpace(cmd.Address.City))
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

                Console.WriteLine($"Address added for customer: {cmd.CustomerId}");
                UntypedPersistentActor.Context.System.EventStream.Publish(e);
            });
        }

        private void Handle(AddPaymentMethod cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                this.Sender.Tell(new CustomerUpdated(this.customerId));
                return;
            }

            if (string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerNotFound(this.customerId));
                return;
            }

            // Basic validation for payment method
            if (string.IsNullOrWhiteSpace(cmd.PaymentMethod.Type))
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

                Console.WriteLine($"Payment method added for customer: {cmd.CustomerId}");
                UntypedPersistentActor.Context.System.EventStream.Publish(e);
            });
        }

        private void Handle(DeactivateCustomer cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                this.Sender.Tell(new CustomerUpdated(this.customerId));
                return;
            }

            if (string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerNotFound(this.customerId));
                return;
            }

            if (this.state.Status == CustomerStatus.Inactive)
            {
                this.Sender.Tell(new CustomerUpdated(this.customerId));
                return;
            }

            var evt = new CustomerDeactivatedEvent(cmd.CustomerId, cmd.Reason, DateTime.UtcNow);

            this.Persist(evt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));

                Console.WriteLine($"Customer deactivated: {cmd.CustomerId}, Reason: {cmd.Reason}");
                UntypedPersistentActor.Context.System.EventStream.Publish(e);
            });
        }

        private bool IsCommandProcessed(string correlationId) =>
            !string.IsNullOrEmpty(correlationId) && this.processedCommands.Contains(correlationId);

        private void MarkCommandProcessed(string correlationId)
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                this.processedCommands.Add(correlationId);
            }
        }

        protected override void PreStart()
        {
            Console.WriteLine($"CustomerActor starting for customer: {this.customerId}");
            base.PreStart();
        }

        protected override void PostStop()
        {
            Console.WriteLine($"CustomerActor stopping for customer: {this.customerId}");
            base.PostStop();
        }
    }
}