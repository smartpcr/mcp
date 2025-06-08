# Migration from Automatonymous to Stateless

## Overview

This guide explains the migration from Automatonymous to Stateless library for state machine implementation in the OrderSystem.

## Key Changes

### 1. **Package Dependencies**
```xml
<!-- Before: Automatonymous -->
<PackageReference Include="Automatonymous" />

<!-- After: Stateless -->
<PackageReference Include="Stateless" />
```

### 2. **State Machine Definition**

#### Before (Automatonymous)
```csharp
public class OrderStateMachine : AutomatonymousStateMachine<OrderSagaData>
{
    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);
        
        Initially(
            When(OrderCreatedEvent)
                .TransitionTo(AwaitingStockReservation)
                .Then(context => 
                {
                    context.Instance.OrderId = context.Data.OrderId;
                    // More initialization...
                })
                .ThenAsync(async context =>
                {
                    await context.Instance.RequestStockReservations(context);
                })
        );
    }
    
    public State AwaitingStockReservation { get; private set; }
    public Event<OrderCreatedData> OrderCreatedEvent { get; private set; }
}
```

#### After (Stateless)
```csharp
public class OrderStateMachineStateless
{
    private readonly StateMachine<OrderStatus, OrderTrigger> _machine;
    private readonly OrderSagaDataStateless _sagaData;
    
    public OrderStateMachineStateless(OrderSagaDataStateless sagaData, IActorContext actorContext)
    {
        _sagaData = sagaData;
        _machine = new StateMachine<OrderStatus, OrderTrigger>(
            () => _sagaData.Status,
            s => _sagaData.Status = s);
        
        ConfigureStateMachine();
    }
    
    private void ConfigureStateMachine()
    {
        _machine.Configure(OrderStatus.Pending)
            .Permit(OrderTrigger.Submit, OrderStatus.AwaitingStockReservation);
            
        _machine.Configure(OrderStatus.AwaitingStockReservation)
            .OnEntryAsync(OnStockReservationRequested)
            .Permit(OrderTrigger.StockReserved, OrderStatus.StockReserved);
    }
}
```

### 3. **Actor Implementation**

#### Before (OrderActor with Automatonymous)
```csharp
public class OrderActor : ReceivePersistentActor
{
    private readonly OrderStateMachine stateMachine;
    private OrderSagaData sagaData;
    
    private void Handle(CreateOrder cmd)
    {
        // Event persistence
        Persist(evt, async e =>
        {
            sagaData = ApplyEvent(sagaData, e);
            
            // Fire state machine event
            await stateMachine.RaiseEvent(sagaData, stateMachine.OrderCreatedEvent, orderCreatedData);
        });
    }
}
```

#### After (OrderActorStateless with Stateless)
```csharp
public class OrderActorStateless : ReceivePersistentActor
{
    private OrderStateMachineStateless? _stateMachine;
    private OrderSagaDataStateless _sagaData;
    
    private void Handle(CreateOrder cmd)
    {
        // Event persistence
        Persist(evt, async e =>
        {
            _sagaData = ApplyEvent(_sagaData, e);
            
            // Initialize and fire state machine trigger
            _stateMachine!.Initialize(orderCreatedData);
            await _stateMachine.FireAsync(OrderTrigger.Submit);
        });
    }
}
```

## Benefits of Migration

### 1. **Reduced Dependencies**
- **Before**: Heavy dependency on MassTransit ecosystem (Automatonymous)
- **After**: Lightweight dependency with no external frameworks

### 2. **Simpler API**
- **Before**: Complex event/state mapping with BehaviorContext
- **After**: Simple trigger-based transitions with clear state management

### 3. **Better Performance**
- **Before**: Additional overhead from Automatonymous abstraction layer
- **After**: Minimal overhead with direct state machine operations

### 4. **Enhanced Debugging**
- **Before**: Complex debugging through Automatonymous internals
- **After**: Clear state transitions with built-in visualization support

### 5. **Explicit State Management**
- **Before**: Implicit state changes through event handlers
- **After**: Explicit state changes through triggers with validation

## Side-by-Side Comparison

| Feature | Automatonymous | Stateless |
|---------|----------------|-----------|
| **Dependencies** | MassTransit ecosystem | Standalone library |
| **Learning Curve** | High (saga patterns) | Low (simple state machine) |
| **State Definition** | Event-driven implicit | Trigger-driven explicit |
| **Visualization** | External tools needed | Built-in DOT graph export |
| **Async Support** | Built-in with contexts | Native async/await |
| **Error Handling** | Exception propagation | Standard try/catch |
| **Testing** | Complex mocking | Simple unit testing |
| **Performance** | Good | Excellent |
| **Memory Usage** | Higher (contexts) | Lower (direct operations) |

## Migration Steps

### Phase 1: Preparation
1. Add Stateless NuGet package
2. Create new Stateless implementations alongside existing
3. Implement comprehensive unit tests

### Phase 2: Implementation
1. Create `OrderStateMachineStateless` class
2. Create `OrderActorStateless` class  
3. Create `OrderSagaDataStateless` data structure
4. Define `OrderTrigger` enum for state transitions

### Phase 3: Testing
1. Run parallel testing with both implementations
2. Validate state transition behavior
3. Performance benchmarking
4. Integration testing

### Phase 4: Deployment
1. Feature flag to switch between implementations
2. Gradual rollout with monitoring
3. Remove Automatonymous implementation after validation

## Code Examples

### State Machine Initialization
```csharp
// Initialize state machine
var sagaData = new OrderSagaDataStateless { OrderId = "123", Status = OrderStatus.Pending };
var stateMachine = new OrderStateMachineStateless(sagaData, actorContext);

// Check current state
Console.WriteLine($"Current State: {stateMachine.CurrentState}");

// Check valid transitions
if (stateMachine.CanFire(OrderTrigger.Submit))
{
    await stateMachine.FireAsync(OrderTrigger.Submit);
}
```

### Visualization
```csharp
// Generate state diagram
var dotGraph = stateMachine.GetDotGraph();
Console.WriteLine(dotGraph);

// Output:
// digraph {
//   Pending -> AwaitingStockReservation [label="Submit"];
//   AwaitingStockReservation -> StockReserved [label="StockReserved"];
//   ...
// }
```

### Error Handling
```csharp
try
{
    await stateMachine.FireAsync(OrderTrigger.Submit);
}
catch (InvalidOperationException ex)
{
    // Handle invalid state transition
    _log.Warning($"Invalid transition: {ex.Message}");
}
```

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task OrderStateMachine_Should_TransitionCorrectly()
{
    // Arrange
    var sagaData = new OrderSagaDataStateless { Status = OrderStatus.Pending };
    var stateMachine = new OrderStateMachineStateless(sagaData, mockContext);
    
    // Act
    await stateMachine.FireAsync(OrderTrigger.Submit);
    
    // Assert
    Assert.Equal(OrderStatus.AwaitingStockReservation, stateMachine.CurrentState);
}
```

### Integration Tests
```csharp
[Fact]
public async Task OrderActor_Should_ProcessOrder_EndToEnd()
{
    // Test complete order flow through all states
    var orderActor = ActorOf<OrderActorStateless>("test-order");
    
    // Create order
    var createCmd = new CreateOrder("customer-1", items, address);
    var result = await orderActor.Ask<OrderCreated>(createCmd);
    
    // Verify state transitions...
}
```

## Monitoring and Observability

### Metrics Collection
```csharp
// Add metrics to state machine
_machine.Configure(OrderStatus.AwaitingStockReservation)
    .OnEntry(() => _metrics.IncrementCounter("orders.stock_reservation_requested"))
    .OnExit(() => _metrics.IncrementCounter("orders.stock_reservation_completed"));
```

### Event History
```csharp
// Track state transitions
public class OrderSagaDataStateless
{
    public List<string> EventHistory { get; set; } = new();
    
    public void AddEvent(string eventDescription)
    {
        EventHistory.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} - {eventDescription}");
    }
}
```

## Conclusion

The migration from Automatonymous to Stateless provides:
- **Reduced complexity** with simpler API
- **Better performance** with minimal overhead  
- **Enhanced maintainability** with explicit state management
- **Improved testability** with straightforward unit testing
- **Better debugging** with clear state transition visibility

The Stateless library offers all the necessary features for our order processing workflow while maintaining compatibility with the existing Akka.NET actor system and event sourcing patterns.