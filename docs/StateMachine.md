# State Machine Design for Order System

## Overview

This document evaluates state machine library options for managing complex workflows in our Akka.NET-based order system, replacing manual state management with declarative state machine definitions.

## State Machine Library Alternatives

### 1. **Stateless** (Recommended)
A lightweight, simple state machine library with no external dependencies.
- **GitHub**: https://github.com/dotnet-state-machine/stateless
- **Stars**: ~5.7k ⭐

**Pros:**
- Very lightweight and performant
- No external dependencies
- Hierarchical states support
- Guards and parameterized triggers
- Export to DOT graph visualization
- Well-maintained with extensive documentation
- Thread-safe

**Cons:**
- No built-in persistence (must implement)
- Less feature-rich than some alternatives
- No saga/workflow orchestration features

**Example:**
```csharp
var orderStateMachine = new StateMachine<OrderStatus, OrderTrigger>(OrderStatus.Initial);

orderStateMachine.Configure(OrderStatus.Initial)
    .Permit(OrderTrigger.Submit, OrderStatus.Submitted);

orderStateMachine.Configure(OrderStatus.Submitted)
    .OnEntry(() => CheckInventory())
    .Permit(OrderTrigger.InventoryReserved, OrderStatus.InventoryReserved)
    .Permit(OrderTrigger.InventoryUnavailable, OrderStatus.Cancelled);

orderStateMachine.Configure(OrderStatus.InventoryReserved)
    .OnEntry(() => ProcessPayment())
    .Permit(OrderTrigger.PaymentSuccess, OrderStatus.PaymentProcessed)
    .Permit(OrderTrigger.PaymentFailed, OrderStatus.Cancelled)
    .OnExit(() => ReleaseInventoryIfCancelled());
```

### 2. **Automatonymous**
Part of the MassTransit ecosystem, designed for complex state machines and sagas.
- **GitHub**: https://github.com/MassTransit/Automatonymous
- **Stars**: ~615 ⭐

**Pros:**
- Declarative API with fluent syntax
- Built-in saga support
- Composite states
- Event correlation
- Timeout handling

**Cons:**
- Heavier dependency (part of MassTransit)
- Steeper learning curve
- More complex for simple use cases

**Example:**
```csharp
public class OrderStateMachine : AutomatonymousStateMachine<OrderState>
{
    public OrderStateMachine()
    {
        Initially(
            When(OrderSubmittedEvent)
                .Then(context => context.Instance.Initialize(context.Data))
                .TransitionTo(Submitted));
        
        During(Submitted,
            When(InventoryReservedEvent)
                .TransitionTo(InventoryReserved));
    }
}
```

### 3. **Appccelerate State Machine**
Feature-rich state machine with async support and reporting.
- **GitHub**: https://github.com/appccelerate/statemachine
- **Stars**: ~438 ⭐

**Pros:**
- Async/await support
- Hierarchical states
- History states
- Built-in reporting and extensions
- Exception handling

**Cons:**
- Less popular/smaller community
- More complex API
- Heavier than alternatives

**Example:**
```csharp
var machine = new PassiveStateMachine<OrderStatus, OrderEvent>();

machine.In(OrderStatus.Initial)
    .On(OrderEvent.Submit).Goto(OrderStatus.Submitted);

machine.In(OrderStatus.Submitted)
    .ExecuteOnEntry(async () => await CheckInventoryAsync())
    .On(OrderEvent.InventoryReserved).Goto(OrderStatus.InventoryReserved);
```

### 4. **Orleans (Microsoft)**
Actor-based framework with built-in state management that could replace Akka.NET.
- **GitHub**: https://github.com/dotnet/orleans
- **Stars**: ~10.1k ⭐

**Pros:**
- Virtual actors with automatic lifecycle
- Built-in persistence
- Distributed by design
- Microsoft support

**Cons:**
- Would require replacing Akka.NET entirely
- Different programming model
- Major architectural change

**Example:**
```csharp
public interface IOrderGrain : IGrainWithStringKey
{
    Task SubmitAsync(OrderData orderData);
    Task ReserveInventoryAsync();
    Task ProcessPaymentAsync();
    Task<OrderStatus> GetStatusAsync();
}

public class OrderGrain : Grain<OrderState>, IOrderGrain
{
    public async Task SubmitAsync(OrderData orderData)
    {
        if (State.Status != OrderStatus.Initial)
            throw new InvalidOperationException("Order already submitted");
        
        State.OrderId = orderData.OrderId;
        State.CustomerId = orderData.CustomerId;
        State.Items = orderData.Items;
        State.Status = OrderStatus.Submitted;
        
        await WriteStateAsync();
        
        // Trigger next step
        var inventoryGrain = GrainFactory.GetGrain<IInventoryGrain>(State.OrderId);
        await inventoryGrain.CheckAvailabilityAsync(State.Items);
    }
    
    public async Task ReserveInventoryAsync()
    {
        if (State.Status != OrderStatus.Submitted)
            throw new InvalidOperationException("Cannot reserve inventory");
        
        State.Status = OrderStatus.InventoryReserved;
        await WriteStateAsync();
        
        // Trigger payment processing
        var paymentGrain = GrainFactory.GetGrain<IPaymentGrain>(State.OrderId);
        await paymentGrain.ProcessAsync(State.TotalAmount);
    }
    
    public async Task ProcessPaymentAsync()
    {
        if (State.Status != OrderStatus.InventoryReserved)
            throw new InvalidOperationException("Invalid state for payment");
        
        State.Status = OrderStatus.PaymentProcessed;
        await WriteStateAsync();
        
        // Continue with shipment...
    }
    
    public Task<OrderStatus> GetStatusAsync() => Task.FromResult(State.Status);
}

public class OrderState
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public List<OrderItem> Items { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Initial;
    public decimal TotalAmount { get; set; }
}
```

### 5. **Custom Implementation**
Build a minimal state machine tailored to specific needs.

**Pros:**
- Exactly what you need
- No external dependencies
- Full control
- Minimal overhead

**Cons:**
- Development and maintenance effort
- Need to implement all features
- Risk of bugs

**Example:**
```csharp
public abstract class StateMachine<TState, TEvent>
    where TState : Enum
    where TEvent : Enum
{
    private readonly Dictionary<(TState, TEvent), StateTransition<TState>> _transitions;
    private readonly List<Action<TState, TState>> _onStateChanged;
    
    protected StateMachine()
    {
        _transitions = new Dictionary<(TState, TEvent), StateTransition<TState>>();
        _onStateChanged = new List<Action<TState, TState>>();
    }
    
    public TState CurrentState { get; private set; }
    
    protected void AddTransition(TState from, TEvent on, TState to, 
        Func<bool> guard = null, Action action = null)
    {
        _transitions[(from, on)] = new StateTransition<TState>(to, guard, action);
    }
    
    protected void OnStateChanged(Action<TState, TState> callback)
    {
        _onStateChanged.Add(callback);
    }
    
    public bool Fire(TEvent eventTrigger)
    {
        if (!_transitions.TryGetValue((CurrentState, eventTrigger), out var transition))
            return false;
        
        if (transition.Guard?.Invoke() == false)
            return false;
        
        var previousState = CurrentState;
        CurrentState = transition.ToState;
        
        transition.Action?.Invoke();
        
        foreach (var callback in _onStateChanged)
            callback(previousState, CurrentState);
        
        return true;
    }
}

public class StateTransition<TState>
{
    public TState ToState { get; }
    public Func<bool> Guard { get; }
    public Action Action { get; }
    
    public StateTransition(TState toState, Func<bool> guard = null, Action action = null)
    {
        ToState = toState;
        Guard = guard;
        Action = action;
    }
}

// Usage example
public class OrderStateMachine : StateMachine<OrderStatus, OrderEvent>
{
    private readonly OrderData _orderData;
    
    public OrderStateMachine(OrderData orderData)
    {
        _orderData = orderData;
        ConfigureTransitions();
    }
    
    private void ConfigureTransitions()
    {
        AddTransition(OrderStatus.Initial, OrderEvent.Submit, OrderStatus.Submitted,
            action: () => CheckInventory());
        
        AddTransition(OrderStatus.Submitted, OrderEvent.InventoryReserved, 
            OrderStatus.InventoryReserved,
            guard: () => _orderData.Items.All(i => i.Available),
            action: () => ProcessPayment());
        
        AddTransition(OrderStatus.Submitted, OrderEvent.InventoryUnavailable, 
            OrderStatus.Cancelled,
            action: () => NotifyCustomer("Inventory unavailable"));
        
        AddTransition(OrderStatus.InventoryReserved, OrderEvent.PaymentSuccess, 
            OrderStatus.PaymentProcessed,
            action: () => CreateShipment());
        
        AddTransition(OrderStatus.InventoryReserved, OrderEvent.PaymentFailed, 
            OrderStatus.Cancelled,
            action: () => ReleaseInventory());
        
        OnStateChanged((from, to) => 
            Console.WriteLine($"Order {_orderData.OrderId}: {from} -> {to}"));
    }
    
    private void CheckInventory() { /* Implementation */ }
    private void ProcessPayment() { /* Implementation */ }
    private void CreateShipment() { /* Implementation */ }
    private void ReleaseInventory() { /* Implementation */ }
    private void NotifyCustomer(string message) { /* Implementation */ }
}

public enum OrderStatus
{
    Initial, Submitted, InventoryReserved, PaymentProcessed, Shipped, Completed, Cancelled
}

public enum OrderEvent
{
    Submit, InventoryReserved, InventoryUnavailable, PaymentSuccess, PaymentFailed, 
    Shipped, Delivered, Cancel
}
```

## Comparison Matrix

| Feature | Stateless | Automatonymous | Appccelerate | Custom |
|---------|-----------|----------------|--------------|--------|
| **Complexity** | Simple | Complex | Medium | Variable |
| **Dependencies** | None | MassTransit | None | None |
| **Performance** | Excellent | Good | Good | Excellent |
| **Hierarchical States** | Yes | Yes | Yes | Manual |
| **Async Support** | Yes | Yes | Yes | Manual |
| **Persistence** | Manual | Built-in | Manual | Manual |
| **Visualization** | DOT export | Via tools | Yes | Manual |
| **Learning Curve** | Low | High | Medium | Low |
| **Community** | Large | Medium | Small | N/A |
| **Akka.NET Integration** | Easy | Medium | Easy | Easy |

## Recommendation: Stateless

For our Akka.NET order system, **Stateless** is the recommended choice because:

1. **Lightweight**: No heavy dependencies, keeping the system lean
2. **Simple Integration**: Easy to integrate with existing Akka.NET actors
3. **Sufficient Features**: Hierarchical states handle our complex workflows
4. **Well-Maintained**: Active development and large community
5. **Performance**: Minimal overhead compared to manual state management

## Implementation Design with Stateless

### Core Components

#### 1. State Machine Definition
```csharp
public class OrderStateMachine
{
    private readonly StateMachine<OrderStatus, OrderTrigger> _machine;
    private readonly OrderState _state;
    
    public OrderStateMachine(OrderState state)
    {
        _state = state;
        _machine = new StateMachine<OrderStatus, OrderTrigger>(
            () => _state.Status,
            s => _state.Status = s);
        
        ConfigureStateMachine();
    }
    
    private void ConfigureStateMachine()
    {
        _machine.Configure(OrderStatus.Initial)
            .Permit(OrderTrigger.Submit, OrderStatus.Submitted);
        
        _machine.Configure(OrderStatus.Submitted)
            .OnEntry(OnOrderSubmitted)
            .Permit(OrderTrigger.InventoryReserved, OrderStatus.InventoryReserved)
            .Permit(OrderTrigger.InventoryUnavailable, OrderStatus.Cancelled);
        
        _machine.Configure(OrderStatus.InventoryReserved)
            .SubstateOf(OrderStatus.Processing)
            .OnEntry(OnInventoryReserved)
            .Permit(OrderTrigger.PaymentSuccess, OrderStatus.PaymentProcessed)
            .Permit(OrderTrigger.PaymentFailed, OrderStatus.Cancelled);
        
        _machine.Configure(OrderStatus.Cancelled)
            .OnEntry(OnOrderCancelled);
    }
    
    public async Task FireAsync(OrderTrigger trigger)
    {
        if (_machine.CanFire(trigger))
        {
            await _machine.FireAsync(trigger);
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot fire {trigger} from {_state.Status}");
        }
    }
}
```

#### 2. State Persistence with Akka.NET
```csharp
public class StateMachineActor : ReceivePersistentActor
{
    private readonly OrderStateMachine _stateMachine;
    private OrderState _state;
    
    public StateMachineActor()
    {
        _state = new OrderState();
        _stateMachine = new OrderStateMachine(_state);
        
        Command<SubmitOrder>(HandleSubmitOrder);
        Command<ReserveInventory>(HandleReserveInventory);
        
        Recover<OrderStateChanged>(evt => 
        {
            _state = evt.NewState;
        });
    }
    
    private void HandleSubmitOrder(SubmitOrder cmd)
    {
        var previousState = _state.Status;
        
        try
        {
            _stateMachine.Fire(OrderTrigger.Submit);
            
            var stateChanged = new OrderStateChanged(
                previousState, 
                _state.Status, 
                cmd);
            
            Persist(stateChanged, evt =>
            {
                Sender.Tell(new OrderSubmitted(_state.OrderId));
            });
        }
        catch (InvalidOperationException ex)
        {
            Sender.Tell(new CommandRejected(ex.Message));
        }
    }
}
```

#### 3. Visualization Support
```csharp
public class StateMachineVisualizer
{
    public static string GenerateDotGraph(OrderStateMachine machine)
    {
        return UmlDotGraph.Format(machine.GetInfo());
    }
}

// Generates:
// digraph {
//   Initial -> Submitted [label="Submit"];
//   Submitted -> InventoryReserved [label="InventoryReserved"];
//   Submitted -> Cancelled [label="InventoryUnavailable"];
//   ...
// }
```

### State Machine Patterns

#### 1. Hierarchical States for Complex Workflows
```csharp
_machine.Configure(OrderStatus.Processing)
    .SubstateOf(OrderStatus.Active)
    .OnEntry(() => _logger.Info("Order processing started"));

_machine.Configure(OrderStatus.InventoryReserved)
    .SubstateOf(OrderStatus.Processing);

_machine.Configure(OrderStatus.PaymentProcessed)
    .SubstateOf(OrderStatus.Processing);
```

#### 2. Guards for Conditional Transitions
```csharp
_machine.Configure(OrderStatus.Submitted)
    .PermitIf(OrderTrigger.InventoryReserved, 
              OrderStatus.InventoryReserved,
              () => _state.Items.All(i => i.Available));
```

#### 3. Parameterized Triggers
```csharp
var cancelTrigger = _machine.SetTriggerParameters<string>(OrderTrigger.Cancel);

_machine.Configure(OrderStatus.Active)
    .Permit(cancelTrigger, OrderStatus.Cancelled)
    .OnEntryFrom(cancelTrigger, reason => 
    {
        _state.CancellationReason = reason;
    });
```

## Migration Strategy

### Phase 1: Proof of Concept
1. Implement PaymentActor with Stateless
2. Compare with existing implementation
3. Validate persistence and recovery

### Phase 2: Infrastructure
1. Create base `StateMachineActor` class
2. Implement state persistence helpers
3. Add monitoring and metrics

### Phase 3: Gradual Migration
1. Migrate one actor type at a time
2. Start with simpler state machines
3. Maintain backward compatibility

### Phase 4: Optimization
1. Fine-tune performance
2. Add visualization tools
3. Implement advanced patterns

## Benefits of This Approach

1. **Simplicity**: Stateless is easy to understand and use
2. **Performance**: Minimal overhead, efficient state management
3. **Flexibility**: Works within existing Akka.NET architecture
4. **Testability**: State machines are easily unit tested
5. **Visualization**: Built-in DOT graph generation
6. **No Heavy Dependencies**: Keeps the system lean

## Conclusion

Using Stateless for state machine implementation provides the best balance of simplicity, features, and performance for our Akka.NET order system. It offers a clean migration path from manual state management while maintaining the benefits of the actor model. The library's maturity and active community ensure long-term support and reliability.