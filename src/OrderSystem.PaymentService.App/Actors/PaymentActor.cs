// -----------------------------------------------------------------------
// <copyright file="PaymentActor.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.PaymentService.App.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Akka.Actor;
    using Akka.Event;
    using Akka.Persistence;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.Contracts.Models;

    public record PaymentState(
        string PaymentId = "",
        string OrderId = "",
        string CustomerId = "",
        decimal Amount = 0m,
        PaymentStatus Status = PaymentStatus.Pending,
        PaymentMethod Method = default,
        string? TransactionId = null,
        string? GatewayResponse = null,
        ImmutableList<PaymentAttempt> Attempts = default,
        DateTime CreatedAt = default,
        DateTime LastUpdated = default)
    {
        public PaymentState() : this(
            PaymentId: "",
            OrderId: "",
            CustomerId: "",
            Amount: 0m,
            Status: PaymentStatus.Pending,
            Method: new PaymentMethod("Unknown"),
            TransactionId: null,
            GatewayResponse: null,
            Attempts: ImmutableList<PaymentAttempt>.Empty,
            CreatedAt: default,
            LastUpdated: default)
        {
        }

        public PaymentState Apply(IPaymentEvent evt) => evt switch
        {
            PaymentInitiatedEvent e => this with
            {
                PaymentId = e.PaymentId,
                OrderId = e.OrderId,
                CustomerId = e.CustomerId,
                Amount = e.Amount,
                Method = e.Method,
                Status = PaymentStatus.Processing,
                CreatedAt = e.InitiatedAt,
                LastUpdated = e.InitiatedAt
            },
            PaymentSucceededEvent e => this with
            {
                Status = PaymentStatus.Succeeded,
                TransactionId = e.TransactionId,
                GatewayResponse = e.GatewayResponse,
                Attempts = this.Attempts.Add(new PaymentAttempt(e.ProcessedAt, true, e.GatewayResponse)),
                LastUpdated = e.ProcessedAt
            },
            PaymentFailedEvent e => this with
            {
                Status = PaymentStatus.Failed,
                GatewayResponse = e.Reason,
                Attempts = this.Attempts.Add(new PaymentAttempt(e.ProcessedAt, false, e.Reason)),
                LastUpdated = e.ProcessedAt
            },
            PaymentRefundedEvent e => this with
            {
                Status = PaymentStatus.Refunded,
                LastUpdated = e.RefundedAt
            },
            _ => this
        };
    }

    public class PaymentActor : ReceivePersistentActor
    {
        private PaymentState state = new();
        private readonly string paymentId;
        private readonly IPaymentGateway gateway;
        private readonly HashSet<string> processedCommands = new();
        private readonly ILoggingAdapter log;
        private ICancelable? retrySchedule;

        public PaymentActor(string paymentId, IPaymentGateway gateway)
        {
            this.log = Context.GetLogger();
            this.paymentId = paymentId;
            this.gateway = gateway;

            this.Command<ProcessPayment>(this.Handle);
            this.Command<RefundPayment>(this.Handle);
            this.Command<RetryPayment>(this.Handle);

            // Handle async operation results
            this.Command<PaymentGatewayResult>(this.HandlePaymentResult);
            this.Command<PaymentTimeout>(this.HandlePaymentTimeout);

            this.Recover<IPaymentEvent>(evt => this.state = this.state.Apply(evt));
            this.Recover<SnapshotOffer>(offer =>
            {
                if (offer.Snapshot is PaymentState state)
                {
                    this.state = state;
                }
            });
        }

        public override string PersistenceId => $"payment-{this.paymentId}";

        private void Handle(ProcessPayment cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                var status = this.state.Status == PaymentStatus.Succeeded;
                this.Sender.Tell(new PaymentResult(this.paymentId, status, this.state.TransactionId, this.state.GatewayResponse));
                return;
            }

            if (!string.IsNullOrEmpty(this.state.PaymentId))
            {
                var status = this.state.Status == PaymentStatus.Succeeded;
                this.Sender.Tell(new PaymentResult(this.paymentId, status, this.state.TransactionId, this.state.GatewayResponse));
                return;
            }

            // Validate payment data
            if (cmd.Amount <= 0)
            {
                this.Sender.Tell(new PaymentResult(this.paymentId, false, null, "Invalid payment amount"));
                return;
            }

            var initiatedEvt = new PaymentInitiatedEvent(cmd.PaymentId, cmd.OrderId, cmd.CustomerId, cmd.Amount, cmd.Method, DateTime.UtcNow);

            this.Persist(initiatedEvt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);

                this.log.Info("Payment initiated: {0} for ${1}", cmd.PaymentId, cmd.Amount);

                // PROPER ASYNC PATTERN: Use PipeTo instead of async void
                this.gateway.ProcessPaymentAsync(cmd.PaymentId, cmd.Amount, cmd.Method)
                    .PipeTo(this.Self,
                            success: result => result,
                            failure: ex => new PaymentGatewayResult(false, null, ex.Message));

                // Set timeout for payment processing using Akka scheduling
                Context.System.Scheduler.ScheduleTellOnce(
                    TimeSpan.FromSeconds(30),
                    this.Self,
                    new PaymentTimeout(),
                    this.Self);
            });
        }

        private void HandlePaymentResult(PaymentGatewayResult result)
        {
            if (result.Success)
            {
                var successEvt = new PaymentSucceededEvent(this.paymentId, this.state.OrderId, result.TransactionId ?? "", result.Response ?? "", DateTime.UtcNow);
                this.Persist(successEvt, e =>
                {
                    this.state = this.state.Apply(e);
                    this.retrySchedule?.Cancel();

                    this.log.Info("Payment succeeded: {0} - {1}", this.paymentId, result.TransactionId);
                    Context.System.EventStream.Publish(e);
                });
            }
            else
            {
                var failedEvt = new PaymentFailedEvent(this.paymentId, this.state.OrderId, result.Reason ?? "Unknown error", DateTime.UtcNow);
                this.Persist(failedEvt, e =>
                {
                    this.state = this.state.Apply(e);
                    this.retrySchedule?.Cancel();

                    this.log.Warning("Payment failed: {0} - {1}", this.paymentId, result.Reason);

                    // Auto-retry for transient failures
                    if (this.state.Attempts.Count < 3 && IsTransientFailure(result.Reason))
                    {
                        this.ScheduleRetry();
                    }
                    else
                    {
                        Context.System.EventStream.Publish(e);
                    }
                });
            }
        }

        private void Handle(RefundPayment cmd)
        {
            if (this.IsCommandProcessed(cmd.CorrelationId))
            {
                this.Sender.Tell(new RefundResult(this.paymentId, true));
                return;
            }

            if (this.state.Status != PaymentStatus.Succeeded)
            {
                this.Sender.Tell(new RefundResult(this.paymentId, false, $"Cannot refund payment with status: {this.state.Status}"));
                return;
            }

            var refundAmount = cmd.Amount ?? this.state.Amount;
            var evt = new PaymentRefundedEvent(this.paymentId, refundAmount, DateTime.UtcNow);

            this.Persist(evt, e =>
            {
                this.state = this.state.Apply(e);
                this.MarkCommandProcessed(cmd.CorrelationId);
                this.Sender.Tell(new RefundResult(this.paymentId, true));

                this.log.Info("Payment refunded: {0} amount ${1}", this.paymentId, refundAmount);
                Context.System.EventStream.Publish(e);
            });
        }

        private void Handle(RetryPayment cmd)
        {
            if (this.state.Attempts.Count >= 3)
            {
                this.Sender.Tell(new PaymentResult(this.paymentId, false, null, "Maximum retry attempts exceeded"));
                return;
            }

            this.log.Info("Retrying payment: {0} (attempt {1})", this.paymentId, this.state.Attempts.Count + 1);

            // Retry the payment using PipeTo pattern
            this.gateway.ProcessPaymentAsync(this.paymentId, this.state.Amount, this.state.Method)
                .PipeTo(this.Self,
                        success: result => result,
                        failure: ex => new PaymentGatewayResult(false, null, ex.Message));
        }

        private void HandlePaymentTimeout(PaymentTimeout timeout)
        {
            if (this.state.Status == PaymentStatus.Processing)
            {
                this.log.Warning("Payment timeout for {0}", this.paymentId);

                var timeoutEvt = new PaymentFailedEvent(this.paymentId, this.state.OrderId, "Payment processing timeout", DateTime.UtcNow);
                this.Persist(timeoutEvt, e =>
                {
                    this.state = this.state.Apply(e);

                    if (this.state.Attempts.Count < 3)
                    {
                        this.ScheduleRetry();
                    }
                    else
                    {
                        Context.System.EventStream.Publish(e);
                    }
                });
            }
        }

        private void ScheduleRetry()
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, this.state.Attempts.Count) * 5); // Exponential backoff
            this.log.Info("Scheduling payment retry for {0} in {1} seconds", this.paymentId, delay.TotalSeconds);

            Context.System.Scheduler.ScheduleTellOnce(
                delay,
                this.Self,
                new RetryPayment(this.paymentId, Guid.NewGuid().ToString()),
                this.Self);
        }

        private static bool IsTransientFailure(string? reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return true;
            }

            var transientKeywords = new[] { "timeout", "network", "temporary", "unavailable", "service" };
            return transientKeywords.Any(keyword => reason.ToLowerInvariant().Contains(keyword));
        }

        private bool IsCommandProcessed(string? correlationId) =>
            !string.IsNullOrEmpty(correlationId) && this.processedCommands.Contains(correlationId);

        private void MarkCommandProcessed(string? correlationId)
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                this.processedCommands.Add(correlationId);
            }
        }

        protected override void PreStart()
        {
            this.log.Info("PaymentActor starting for payment: {0}", this.paymentId);
            base.PreStart();
        }

        protected override void PostStop()
        {
            this.retrySchedule?.Cancel();
            this.log.Info("PaymentActor stopping for payment: {0}", this.paymentId);
            base.PostStop();
        }
    }

    // Internal message for payment timeout
    public record PaymentTimeout();
}