# Persistence Strategy for OrderSystem

## Overview

This document outlines the persistence strategy for the OrderSystem microservices architecture, focusing on open source, cross-platform solutions that support event sourcing and actor-based patterns with Akka.NET.

## Requirements

- **Open Source Only**: No commercial or proprietary solutions
- **Cross-Platform**: Must run on both Linux and Windows
- **Event Sourcing Compatible**: Support for event-driven architectures
- **Akka.NET Integration**: Compatible with actor persistence patterns
- **High Availability**: Support for clustering and replication
- **Performance**: Suitable for microservices workloads

## Evaluated Options

### Event Stores (Purpose-Built)

#### 1. Marten + PostgreSQL ⭐ **RECOMMENDED**
- **License**: MIT (Open Source)
- **Platform**: Linux/Windows ✅
- **Integration**: Native .NET, excellent Akka.NET support
- **Strengths**:
  - Purpose-built for event sourcing in .NET
  - Leverages PostgreSQL's ACID guarantees and performance
  - Excellent documentation and .NET community support
  - Built-in projections and read model support
  - JSON document capabilities with relational features
- **Weaknesses**:
  - Requires PostgreSQL knowledge
  - Single database dependency
- **Use Case**: Primary event store for all domain events

#### 2. EventStoreDB
- **License**: Mozilla Public License 2.0
- **Platform**: Linux/Windows ✅
- **Integration**: Official .NET client, Akka.Persistence plugins
- **Strengths**:
  - Industry-standard event store
  - Optimized for event streaming
  - Built-in projections and subscriptions
- **Weaknesses**:
  - Additional infrastructure component
  - Learning curve for teams new to event stores
- **Use Case**: Alternative if dedicated event store is needed

### Traditional Databases

#### 3. PostgreSQL
- **License**: PostgreSQL License (Open Source)
- **Platform**: Linux/Windows ✅
- **Integration**: Npgsql, Akka.Persistence.PostgreSql
- **Strengths**:
  - Mature, reliable, and performant
  - Excellent JSON support for documents
  - Strong consistency and ACID compliance
  - Extensive ecosystem and tooling
- **Weaknesses**:
  - Requires custom event sourcing implementation
- **Use Case**: Foundational database for all services

#### 4. MariaDB
- **License**: GPL v2 (Open Source)
- **Platform**: Linux/Windows ✅
- **Integration**: MySqlConnector, Akka.Persistence.MySql
- **Strengths**:
  - Drop-in MySQL replacement with better performance
  - Good clustering support
- **Weaknesses**:
  - Less JSON support than PostgreSQL
  - Custom event sourcing needed
- **Use Case**: Alternative to PostgreSQL for teams with MySQL experience

### Document Stores

#### 5. MongoDB Community Edition
- **License**: SSPL (Open Source but restrictive)
- **Platform**: Linux/Windows ✅
- **Integration**: MongoDB.Driver, Akka.Persistence.MongoDB
- **Strengths**:
  - Schema flexibility
  - Excellent horizontal scaling
  - Good for rapid development
- **Weaknesses**:
  - SSPL license restrictions for cloud deployment
  - Eventual consistency by default
- **Use Case**: Consider only if document flexibility is critical

### Key-Value Stores

#### 6. Valkey ⭐ **RECOMMENDED for Caching**
- **License**: BSD 3-Clause (Open Source)
- **Platform**: Linux/Windows ✅
- **Integration**: StackExchange.Redis (Redis-compatible)
- **Strengths**:
  - Linux Foundation backed
  - Redis compatibility
  - In-memory performance with persistence
- **Weaknesses**:
  - Not suitable as primary store
- **Use Case**: Caching, session storage, pub/sub

#### 7. SQLite
- **License**: Public Domain
- **Platform**: Linux/Windows ✅
- **Integration**: Microsoft.Data.Sqlite, Akka.Persistence.Sqlite
- **Strengths**:
  - Zero configuration
  - Excellent for development/testing
  - Single file deployment
- **Weaknesses**:
  - Single writer limitation
  - Not suitable for production clustering
- **Use Case**: Development, testing, single-node deployments

## Recommended Architecture

### Primary Strategy: Marten + PostgreSQL + Valkey

```
┌─────────────────────────────────────────────────────────────┐
│                     OrderSystem Architecture                │
├─────────────────────────────────────────────────────────────┤
│  Services: Catalog | Customer | Order | Payment | Shipment  │
└─────────────┬───────────────────────────────────────────────┘
              │
┌─────────────▼───────────────────────────────────────────────┐
│                    Persistence Layer                        │
├─────────────────────────────────────────────────────────────┤
│  Event Store: Marten + PostgreSQL                          │
│  • Domain events (OrderCreated, PaymentProcessed, etc.)    │
│  • Actor state persistence                                 │
│  • Projections and read models                             │
│                                                             │
│  Cache: Valkey                                              │
│  • Session data                                             │
│  • Frequently accessed aggregates                          │
│  • Cross-service communication cache                       │
│                                                             │
│  Development: SQLite                                        │
│  • Local development environment                           │
│  • Unit testing                                            │
│  • CI/CD pipeline testing                                  │
└─────────────────────────────────────────────────────────────┘
```

### Service-Specific Persistence Patterns

#### Catalog Service
```csharp
// Event Storage with Marten
public class ProductAggregate
{
    public void Apply(ProductCreated @event) { /* ... */ }
    public void Apply(ProductUpdated @event) { /* ... */ }
    public void Apply(ProductDeactivated @event) { /* ... */ }
}

// Read Model Projections
public class ProductCatalogProjection : IProjection
{
    public void Apply(ProductCreated @event) 
    {
        // Project to read-optimized view
    }
}
```

#### Order Service
```csharp
// Complex aggregate with state machine
public class OrderAggregate
{
    public OrderStatus Status { get; private set; }
    
    public void Apply(OrderCreated @event) { /* ... */ }
    public void Apply(OrderItemAdded @event) { /* ... */ }
    public void Apply(OrderSubmitted @event) { /* ... */ }
    public void Apply(OrderCompleted @event) { /* ... */ }
}
```

#### Customer Service
```csharp
// Customer data with event sourcing
public class CustomerAggregate
{
    public void Apply(CustomerRegistered @event) { /* ... */ }
    public void Apply(CustomerUpdated @event) { /* ... */ }
    public void Apply(CustomerAddressChanged @event) { /* ... */ }
}
```

## Implementation Strategy

### Phase 1: Foundation Setup
1. **PostgreSQL Infrastructure**
   - Set up PostgreSQL clusters for production
   - Configure replication and backup strategies
   - Establish connection pooling

2. **Marten Integration**
   - Install Marten NuGet packages
   - Configure event store schemas
   - Set up basic projections

3. **Akka.NET Integration**
   - Configure Akka.Persistence.PostgreSql
   - Set up actor state persistence
   - Configure cluster sharding with PostgreSQL

### Phase 2: Event Sourcing Implementation
1. **Domain Events**
   - Define event schemas for each service
   - Implement event versioning strategy
   - Create event serialization

2. **Aggregates**
   - Implement aggregate roots
   - Add event application logic
   - Configure aggregate persistence

3. **Projections**
   - Create read model projections
   - Implement projection rebuilding
   - Set up projection monitoring

### Phase 3: Performance and Scaling
1. **Caching Layer**
   - Deploy Valkey clusters
   - Implement cache-aside pattern
   - Add cache invalidation strategies

2. **Read Optimization**
   - Optimize projection queries
   - Add database indexes
   - Implement query optimization

3. **Monitoring**
   - Add persistence metrics
   - Monitor event store performance
   - Set up alerting

## Configuration Examples

### Marten Configuration
```csharp
services.AddMarten(options =>
{
    options.Connection(connectionString);
    
    // Event Store Configuration
    options.Events.DatabaseSchemaName = "events";
    options.Events.AppendMode = EventAppendMode.Rich;
    
    // Document Store Configuration
    options.Schema.For<Order>().DocumentAlias("orders");
    options.Schema.For<Customer>().DocumentAlias("customers");
    
    // Projections
    options.Projections.Add<OrderSummaryProjection>(ProjectionLifecycle.Async);
    options.Projections.Add<CustomerSummaryProjection>(ProjectionLifecycle.Async);
});
```

### Akka.NET Persistence Configuration
```csharp
services.ConfigureOrderSystemAkka(configuration, (builder, serviceProvider) =>
{
    builder.WithJournaling("akka.persistence.journal.postgresql", 
        new PostgreSqlJournalOptions
        {
            ConnectionString = connectionString,
            SchemaName = "akka_journal"
        });
        
    builder.WithSnapshots("akka.persistence.snapshot-store.postgresql",
        new PostgreSqlSnapshotOptions
        {
            ConnectionString = connectionString,
            SchemaName = "akka_snapshots"
        });
});
```

### Valkey Caching Configuration
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "OrderSystem";
});

// Usage in services
public class OrderService
{
    private readonly IDistributedCache _cache;
    
    public async Task<Order> GetOrderAsync(string orderId)
    {
        var cached = await _cache.GetStringAsync($"order:{orderId}");
        if (cached != null)
            return JsonSerializer.Deserialize<Order>(cached);
            
        var order = await _orderRepository.GetAsync(orderId);
        await _cache.SetStringAsync($"order:{orderId}", 
            JsonSerializer.Serialize(order),
            TimeSpan.FromMinutes(15));
            
        return order;
    }
}
```

## Environment-Specific Configurations

### Development Environment
- **Primary**: SQLite for simplicity
- **Alternative**: Docker PostgreSQL + Valkey
- **Benefits**: Zero configuration, fast startup

### Testing Environment
- **Primary**: In-memory SQLite
- **Alternative**: TestContainers with PostgreSQL
- **Benefits**: Isolated tests, fast execution

### Production Environment
- **Primary**: PostgreSQL cluster with read replicas
- **Caching**: Valkey cluster
- **Benefits**: High availability, performance, scalability

## Migration Strategy

### From Current Azure Storage
1. **Parallel Implementation**
   - Run both persistence systems in parallel
   - Gradually migrate aggregates to new system
   - Validate data consistency

2. **Event Migration**
   - Export existing events from Azure
   - Transform to new event schema
   - Import into Marten event store

3. **Rollback Plan**
   - Maintain Azure persistence as fallback
   - Implement feature flags for persistence switching
   - Monitor performance and stability

## Performance Considerations

### PostgreSQL Optimization
- Use appropriate indexes for event queries
- Configure connection pooling (recommended: 10-20 connections per service)
- Enable query logging for optimization
- Regular VACUUM and ANALYZE operations

### Marten Optimization
- Use async projections for read models
- Batch event appending when possible
- Configure appropriate JSON serialization settings
- Monitor projection lag and rebuild capabilities

### Valkey Optimization
- Configure appropriate memory limits
- Use appropriate data structures (hashes for objects, sets for collections)
- Implement cache warming strategies
- Monitor cache hit ratios

## Security Considerations

### Database Security
- Use connection string encryption
- Implement role-based access control
- Enable SSL/TLS for all connections
- Regular security updates and patches

### Event Store Security
- Encrypt sensitive data in events
- Implement event-level access control
- Audit event access and modifications
- Secure backup and restore procedures

## Monitoring and Alerting

### Key Metrics
- Event store write/read latency
- Projection processing lag
- Cache hit/miss ratios
- Database connection pool utilization
- Storage growth rates

### Alerting Thresholds
- Event append failures
- Projection rebuild failures
- High database connection usage
- Cache cluster failures
- Disk space utilization

## Backup and Disaster Recovery

### PostgreSQL Backup Strategy
- Continuous WAL archiving
- Daily base backups
- Point-in-time recovery capability
- Cross-region backup replication

### Event Store Recovery
- Event stream backup validation
- Projection rebuild procedures
- Aggregate state recovery testing
- Disaster recovery runbooks

## Cost Considerations

### Infrastructure Costs
- PostgreSQL: VM/container hosting costs
- Valkey: Memory and compute costs
- Storage: Event data growth over time
- Backup: Long-term storage costs

### Operational Costs
- Monitoring and alerting tools
- Database administration time
- Performance tuning efforts
- Security maintenance

## Conclusion

The recommended persistence strategy using Marten + PostgreSQL + Valkey provides:

- ✅ **Open Source**: All components are truly open source
- ✅ **Cross-Platform**: Full Linux and Windows support
- ✅ **Event Sourcing**: Purpose-built capabilities with Marten
- ✅ **Performance**: Proven scalability and performance characteristics
- ✅ **Maintainability**: Excellent .NET ecosystem integration
- ✅ **Cost-Effective**: No licensing fees, standard infrastructure

This architecture supports the OrderSystem's event-driven microservices while providing flexibility for future growth and scaling requirements.