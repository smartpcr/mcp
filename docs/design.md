# Actor-Based Order System Design

## Overview

This document outlines the design for a proof-of-concept (POC) order management system using the Actor Model pattern with Akka.NET. The system avoids traditional distributed transactions by leveraging actors' single-threaded execution model and message-passing architecture.

## Core Principles

- **No Distributed Transactions**: Instead of 2PC or saga patterns, we use eventual consistency through actor messaging
- **Single-Threaded Actors**: Each actor processes one message at a time, eliminating the need for locks
- **Message-Driven**: All communication happens through immutable messages
- **Event Sourcing**: Actors maintain state through event streams for recovery and audit

## System Architecture

### Actor Hierarchy

```
/user
├── order-service
│   └── order-{orderId}
├── payment-service
│   └── payment-{paymentId}
├── account-service
│   └── account-{accountId}
├── catalog-service
│   └── product-{productId}
└── shipment-service
    └── shipment-{shipmentId}
```

### Service Actors

#### 1. Order Service Actor
**Responsibilities:**
- Creates and manages individual order actors
- Routes messages to appropriate order instances
- Maintains order lifecycle state machine

**Messages:**
- `CreateOrder(customerId, items, shippingAddress)`
- `OrderCreated(orderId, customerId, items, totalAmount)`
- `UpdateOrderStatus(orderId, status)`
- `CancelOrder(orderId, reason)`

**State Transitions:**
```
Pending → PaymentProcessing → PaymentConfirmed → Preparing → Shipped → Delivered
     ↓            ↓                    ↓              ↓          ↓
  Cancelled    PaymentFailed      OutOfStock    ShipmentFailed  ReturnRequested
```

#### 2. Payment Service Actor
**Responsibilities:**
- Processes payment requests
- Manages payment state and retries
- Communicates with external payment gateways

**Messages:**
- `ProcessPayment(orderId, accountId, amount, paymentMethod)`
- `PaymentSucceeded(paymentId, orderId, transactionId)`
- `PaymentFailed(paymentId, orderId, reason)`
- `RefundPayment(paymentId, amount)`

#### 3. Account Service Actor
**Responsibilities:**
- Manages customer account balances
- Tracks transaction history
- Validates account status

**Messages:**
- `ValidateAccount(accountId)`
- `DebitAccount(accountId, amount, orderId)`
- `CreditAccount(accountId, amount, reason)`
- `AccountDebited(accountId, newBalance)`
- `InsufficientFunds(accountId, requestedAmount)`

#### 4. Catalog Service Actor
**Responsibilities:**
- Manages product inventory
- Reserves items for orders
- Handles stock updates

**Messages:**
- `CheckAvailability(productId, quantity)`
- `ReserveItems(orderId, items)`
- `ReleaseReservation(orderId, items)`
- `UpdateStock(productId, quantity)`
- `ItemsReserved(orderId, reservationId)`
- `ItemsUnavailable(productId, requestedQty, availableQty)`

#### 5. Shipment Service Actor
**Responsibilities:**
- Creates shipping labels
- Tracks shipment status
- Coordinates with carriers

**Messages:**
- `CreateShipment(orderId, items, address)`
- `UpdateShipmentStatus(shipmentId, status, location)`
- `ShipmentCreated(shipmentId, orderId, trackingNumber)`
- `ShipmentDelivered(shipmentId, orderId, deliveryTime)`

## Order Flow Sequence

### Happy Path
```
1. Customer → OrderService: CreateOrder
2. OrderService → OrderActor: Create new actor
3. OrderActor → CatalogService: CheckAvailability
4. CatalogService → OrderActor: ItemsAvailable
5. OrderActor → CatalogService: ReserveItems
6. CatalogService → OrderActor: ItemsReserved
7. OrderActor → AccountService: ValidateAccount
8. AccountService → OrderActor: AccountValid
9. OrderActor → PaymentService: ProcessPayment
10. PaymentService → AccountService: DebitAccount
11. AccountService → PaymentService: AccountDebited
12. PaymentService → OrderActor: PaymentSucceeded
13. OrderActor → ShipmentService: CreateShipment
14. ShipmentService → OrderActor: ShipmentCreated
15. OrderActor → Customer: OrderConfirmed
```

### Compensation Flow (Payment Failure)
```
1. PaymentService → OrderActor: PaymentFailed
2. OrderActor → CatalogService: ReleaseReservation
3. CatalogService → OrderActor: ReservationReleased
4. OrderActor → Customer: OrderCancelled(PaymentFailed)
```

## State Management

### Actor State Persistence
Each actor maintains its state using Akka.Persistence:

```csharp
public class OrderActor : ReceivePersistentActor
{
    private OrderState _state = new OrderState();
    
    public override string PersistenceId => $"order-{_orderId}";
    
    private void UpdateState(IOrderEvent evt)
    {
        _state = _state.Apply(evt);
    }
}
```

### Event Types
```csharp
public interface IOrderEvent { }

public record OrderCreatedEvent(
    string OrderId,
    string CustomerId,
    List<OrderItem> Items,
    decimal TotalAmount,
    DateTime CreatedAt
) : IOrderEvent;

public record PaymentProcessedEvent(
    string OrderId,
    string PaymentId,
    PaymentStatus Status
) : IOrderEvent;

public record ItemsReservedEvent(
    string OrderId,
    string ReservationId,
    List<OrderItem> Items
) : IOrderEvent;
```

## Consistency Guarantees

### Eventual Consistency
- Each service maintains its own consistent state
- Cross-service consistency achieved through messaging
- Compensating actions handle failure scenarios

### Idempotency
All message handlers must be idempotent:
```csharp
public void Handle(ProcessPayment msg)
{
    if (_processedPayments.Contains(msg.PaymentId))
    {
        Sender.Tell(new PaymentAlreadyProcessed(msg.PaymentId));
        return;
    }
    // Process payment...
}
```

### Timeout Handling
```csharp
Context.SetReceiveTimeout(TimeSpan.FromSeconds(30));
Receive<ReceiveTimeout>(_ => 
{
    // Trigger compensation or retry logic
    Self.Tell(new CheckOrderStatus(_orderId));
});
```

## Failure Handling

### Actor Supervision
```csharp
public class ServiceSupervisor : ReceiveActor
{
    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(
            maxNrOfRetries: 3,
            withinTimeRange: TimeSpan.FromMinutes(1),
            localOnlyDecider: ex => ex switch
            {
                TransientException => Directive.Restart,
                PersistenceException => Directive.Stop,
                _ => Directive.Escalate
            });
    }
}
```

### Retry Strategies
- Exponential backoff for external service calls
- Circuit breaker pattern for payment gateway
- Dead letter queue for unprocessable messages

## Monitoring and Observability

### Actor Metrics
- Message processing time
- Queue depth per actor
- Failure rates
- State transition counts

### Distributed Tracing
- Correlation IDs flow through all messages
- OpenTelemetry integration for trace visualization

### Event Log
All state changes persisted as events provide complete audit trail:
```
OrderCreated → ItemsReserved → PaymentProcessed → ShipmentCreated → OrderDelivered
```

## Implementation Guidelines

### Message Design
```csharp
public record CreateOrder(
    string CustomerId,
    List<OrderItem> Items,
    Address ShippingAddress,
    string CorrelationId = null
) : ICommand
{
    public string CorrelationId { get; init; } = CorrelationId ?? Guid.NewGuid().ToString();
}
```

### Actor Creation
```csharp
var orderActor = Context.ActorOf(
    Props.Create(() => new OrderActor(orderId)),
    name: $"order-{orderId}"
);
```

### Testing Strategy
- Unit tests for individual actor behavior
- Integration tests using Akka.TestKit
- Chaos testing for failure scenarios

## Benefits of This Approach

1. **No Locks**: Single-threaded actors eliminate race conditions
2. **Scalability**: Actors can be distributed across nodes
3. **Resilience**: Supervisor hierarchies handle failures gracefully
4. **Auditability**: Event sourcing provides complete history
5. **Flexibility**: Easy to add new services or modify flows

## Considerations

1. **Eventual Consistency**: UI must handle intermediate states
2. **Message Ordering**: Use sequence numbers where strict ordering required
3. **Persistence**: Choose appropriate event store (EventStore, Kafka, etc.)
4. **Cluster Management**: Consider Akka.Cluster for production deployment