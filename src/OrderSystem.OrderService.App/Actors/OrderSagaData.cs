// -----------------------------------------------------------------------
// <copyright file="OrderSagaData.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.OrderService.App.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Akka.Actor;
    using Automatonymous;
    using OrderSystem.Contracts.Models;

    public class OrderSagaData
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = "";

        // Order Data
        public string OrderId { get; set; } = "";
        public string CustomerId { get; set; } = "";
        public List<OrderItem> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public OrderSystem.Contracts.Models.Address ShippingAddress { get; set; } = new("", "", "", "", "");
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        // Process Data
        public string? PaymentId { get; set; }
        public string? ShipmentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public HashSet<string> ReservedProducts { get; set; } = new();
        public Dictionary<string, string> ProcessedCommands { get; set; } = new();

        // Actor Context for publishing events
        public IActorContext? ActorContext { get; set; }

        public async Task RequestStockReservations(BehaviorContext<OrderSagaData> context)
        {
            foreach (var item in Items)
            {
                var reserveStockCmd = new ReserveStock(item.ProductId, OrderId, item.Quantity, OrderId);
                ActorContext?.System.EventStream.Publish(reserveStockCmd);
            }
            await Task.CompletedTask;
        }

        public async Task RequestPaymentProcessing(BehaviorContext<OrderSagaData> context)
        {
            PaymentId = Guid.NewGuid().ToString();
            LastUpdated = DateTime.UtcNow;
            
            var processPaymentCmd = new ProcessPayment(
                PaymentId, 
                OrderId, 
                CustomerId, 
                TotalAmount, 
                new PaymentMethod("CreditCard"), 
                OrderId);
                
            ActorContext?.System.EventStream.Publish(processPaymentCmd);
            await Task.CompletedTask;
        }

        public async Task RequestShipment(BehaviorContext<OrderSagaData> context)
        {
            ShipmentId = Guid.NewGuid().ToString();
            LastUpdated = DateTime.UtcNow;
            
            var createShipmentCmd = new CreateShipment(
                ShipmentId, 
                OrderId, 
                Items, 
                ShippingAddress, 
                OrderId);
                
            ActorContext?.System.EventStream.Publish(createShipmentCmd);
            await Task.CompletedTask;
        }

        public async Task HandleFailureCompensation(BehaviorContext<OrderSagaData> context)
        {
            // Release stock reservations
            foreach (var productId in ReservedProducts)
            {
                var releaseStockCmd = new ReleaseStock(productId, OrderId, OrderId);
                ActorContext?.System.EventStream.Publish(releaseStockCmd);
            }

            // Refund payment if it was processed
            if (!string.IsNullOrEmpty(PaymentId))
            {
                var refundPaymentCmd = new RefundPayment(PaymentId, null, OrderId);
                ActorContext?.System.EventStream.Publish(refundPaymentCmd);
            }

            Status = OrderStatus.PaymentFailed;
            LastUpdated = DateTime.UtcNow;
            await Task.CompletedTask;
        }

        public async Task HandleCancellationCompensation(BehaviorContext<OrderSagaData> context)
        {
            await HandleFailureCompensation(context);
            Status = OrderStatus.Cancelled;
        }
    }

}