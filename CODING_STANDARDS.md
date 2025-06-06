# Coding Standards

This document outlines the coding standards and patterns to follow in this project.

## General Rules

### 1. Async/Await Patterns

**NEVER use `async void`** - The only exception is event handlers.

**Good:**
```csharp
// Use async Task for methods that don't return a value
public async Task ProcessOrderAsync(Order order)
{
    await this.repository.SaveAsync(order).ConfigureAwait(false);
}

// Use async Task<T> for methods that return a value
public async Task<Order> GetOrderAsync(string orderId)
{
    return await this.repository.GetByIdAsync(orderId).ConfigureAwait(false);
}
```

**Bad:**
```csharp
// NEVER do this - async void
public async void ProcessOrder(Order order)  // ❌ BAD
{
    await this.repository.SaveAsync(order);
}
```

### 2. Fire-and-Forget in Actors

In Akka.NET actors, for fire-and-forget async operations, use `PipeTo` pattern instead of `async void`:

**Good:**
```csharp
// Use PipeTo pattern for async operations in actors
private void Handle(ProcessPayment cmd)
{
    this.gateway.ProcessPaymentAsync(cmd.PaymentId, cmd.Amount)
        .PipeTo(this.Self, success: result => new PaymentProcessed(result),
                failure: ex => new PaymentFailed(ex.Message));
}
```

**Bad:**
```csharp
// NEVER do this in actors
private async void Handle(ProcessPayment cmd)  // ❌ BAD
{
    try
    {
        var result = await this.gateway.ProcessPaymentAsync(cmd.PaymentId, cmd.Amount);
        this.Self.Tell(new PaymentProcessed(result));
    }
    catch (Exception ex)
    {
        this.Self.Tell(new PaymentFailed(ex.Message));
    }
}
```

### 3. ConfigureAwait(false)

Always use `ConfigureAwait(false)` in library code to avoid deadlocks:

```csharp
public async Task<Result> ProcessAsync()
{
    var data = await this.service.GetDataAsync().ConfigureAwait(false);
    return await this.processor.ProcessAsync(data).ConfigureAwait(false);
}
```

### 4. Exception Handling in Async Code

**Good:**
```csharp
public async Task<Result> ProcessAsync()
{
    try
    {
        return await this.service.ProcessAsync().ConfigureAwait(false);
    }
    catch (SpecificException ex)
    {
        this.logger.LogError(ex, "Specific error occurred");
        throw; // Re-throw to preserve stack trace
    }
}
```

### 5. Task Return Patterns

**For methods that only await and return:**
```csharp
// Good - no async/await needed
public Task<Order> GetOrderAsync(string id)
{
    return this.repository.GetByIdAsync(id);
}

// Also good when you need ConfigureAwait
public async Task<Order> GetOrderAsync(string id)
{
    return await this.repository.GetByIdAsync(id).ConfigureAwait(false);
}
```

## Akka.NET Specific Patterns

### 1. Async Operations in Actors

Use the `PipeTo` pattern for integrating async operations:

```csharp
private void Handle(LoadData cmd)
{
    this.dataService.LoadAsync(cmd.Id)
        .PipeTo(this.Self, 
                success: data => new DataLoaded(data),
                failure: ex => new DataLoadFailed(ex.Message));
}

// Handle the results
this.Command<DataLoaded>(result => 
{
    // Process successful result
});

this.Command<DataLoadFailed>(failure => 
{
    // Handle failure
});
```

### 2. Scheduling in Actors

**Good:**
```csharp
// Use Akka's scheduling mechanisms
this.Context.System.Scheduler.ScheduleTellOnce(
    TimeSpan.FromSeconds(30),
    this.Self,
    new ProcessTimeout(),
    this.Self);
```

### 3. Actor State Management

Always ensure actor state is updated in a synchronous, single-threaded manner:

```csharp
private void Handle(UpdateState cmd)
{
    // Synchronous state update
    this.state = this.state.Apply(cmd);
    
    // Then trigger async operation if needed
    this.externalService.NotifyAsync(this.state.Id)
        .PipeTo(this.Self,
                success: () => new NotificationSent(),
                failure: ex => new NotificationFailed(ex.Message));
}
```

## Member Access Patterns

### 1. Instance vs Static Member Access

**Use `this` qualifier for instance members:**
```csharp
public class MyClass
{
    private readonly string instanceField;
    private static readonly string StaticField = "value";
    
    public void InstanceMethod()
    {
        // ✅ GOOD: Use this for instance members
        var value = this.instanceField;
        this.SomeInstanceMethod();
        
        // ✅ GOOD: No this for static members
        var staticValue = StaticField;
        StaticMethod();
    }
    
    private void SomeInstanceMethod() { }
    private static void StaticMethod() { }
}
```

**Bad examples:**
```csharp
// ❌ BAD: Using this with static members
var badValue = this.StaticField;  // Static field
this.StaticMethod();              // Static method

// ❌ BAD: Not using this with instance members (against our standard)
var value = instanceField;        // Should be this.instanceField
SomeInstanceMethod();             // Should be this.SomeInstanceMethod()
```

## Code Quality Rules

1. **No `async void`** except for event handlers
2. **Always use `ConfigureAwait(false)`** in library code
3. **Use `PipeTo`** for async operations in actors
4. **Properly handle exceptions** in async code
5. **Use Task return types** appropriately
6. **Avoid fire-and-forget** patterns
7. **Use `this` qualifier for instance members only** (not static)

These patterns ensure thread safety, proper exception handling, and maintainable async code.