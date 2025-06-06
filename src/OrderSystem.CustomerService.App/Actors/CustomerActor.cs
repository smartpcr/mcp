// -----------------------------------------------------------------------
// <copyright file="CustomerActor.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.CustomerService.App.Actors
{
    using System;
    using System.Collections.Generic;
    using Akka.Actor;
    using Akka.Event;
    using Akka.Persistence;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.Contracts.Models;
    public class CustomerActor : ReceivePersistentActor
    {
        private CustomerState state = new();
        private readonly string customerId;
        private readonly HashSet<string> processedCommands = new();
        private readonly ILoggingAdapter log;

        public CustomerActor(string customerId)
        {
            this.log = UntypedPersistentActor.Context.GetLogger();
            this.customerId = customerId;

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

        public override string PersistenceId => $"customer-{this.customerId}";

        private bool IsCommandProcessed(string? correlationId)
        {
            if (string.IsNullOrEmpty(correlationId))
            {
                return false; // If no correlationId, cannot check for duplicates effectively this way
            }

            if (this.processedCommands.Contains(correlationId))
            {
                this.log.Info("Command with correlation ID [{0}] already processed.", correlationId);
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

        private void Handle(CreateCustomer cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                // If already processed, and we assume the initial reply was CustomerCreated,
                // we might send it again or a generic ack.
                this.Sender.Tell(new CustomerCreated(cmd.CustomerId));
                return;
            }

            if (!string.IsNullOrEmpty(this.state.CustomerId))
            {
                this.Sender.Tell(new CustomerAlreadyExists(this.customerId));
                return;
            }

            var evt = new CustomerCreatedEvent(cmd.CustomerId, cmd.Email, cmd.Name, DateTime.UtcNow);

            this.Persist(evt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(new CustomerCreated(cmd.CustomerId));
                UntypedPersistentActor.Context.System.EventStream.Publish(e);
                // Consider snapshotting periodically
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

            var evt = new CustomerUpdatedEvent(cmd.CustomerId, cmd.Name, cmd.Email, DateTime.UtcNow);

            this.Persist(evt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));
                UntypedPersistentActor.Context.System.EventStream.Publish(e);
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

            var evt = new AddressAddedEvent(cmd.CustomerId, cmd.Address, DateTime.UtcNow);
            this.Persist(evt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));
                UntypedPersistentActor.Context.System.EventStream.Publish(e);
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

            var evt = new PaymentMethodAddedEvent(cmd.CustomerId, cmd.PaymentMethod, DateTime.UtcNow);
            this.Persist(evt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));
                UntypedPersistentActor.Context.System.EventStream.Publish(e);
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
                // Already inactive, consider this a success for idempotency
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));
                return;
            }

            var evt = new CustomerDeactivatedEvent(cmd.CustomerId, cmd.Reason, DateTime.UtcNow);
            this.Persist(evt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(new CustomerUpdated(cmd.CustomerId));
                UntypedPersistentActor.Context.System.EventStream.Publish(e);
            });
        }

        private void Handle(ValidateCustomer cmd)
        {
            // This is a query, no need to check IsCommandProcessed or persist events.
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
            this.Sender.Tell(result);
        }
    }
}
