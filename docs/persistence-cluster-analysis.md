# Persistence Cluster and Backup Analysis

## Current System Review

Based on the code analysis, here's the current persistence setup and recommendations for clustered environments:

### Current Implementation
- **Persistence**: Azure Table Storage (via `Akka.Persistence.Azure`)
- **Discovery**: Azure Table Storage for cluster discovery
- **Clustering**: Akka.NET clustering with Akka.Management
- **Backup**: Not explicitly configured (relies on Azure Storage built-in redundancy)

### Recommended Implementation (per persistence.md)
- **Primary**: Marten + PostgreSQL
- **Cache**: Valkey (Redis fork)
- **Development**: SQLite

## Cluster Environment Support Analysis

### ✅ PostgreSQL + Marten - EXCELLENT CLUSTER SUPPORT

#### **High Availability & Clustering**
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   PostgreSQL    │    │   PostgreSQL    │    │   PostgreSQL    │
│   Primary       │◄──►│   Replica 1     │◄──►│   Replica 2     │
│                 │    │   (Read Only)   │    │   (Read Only)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                    ┌─────────────▼──────────────┐
                    │      Load Balancer         │
                    │   (PgBouncer/HAProxy)      │
                    └────────────────────────────┘
```

**Supported Clustering Features:**
- ✅ **Streaming Replication**: Real-time data replication to multiple nodes
- ✅ **Read Replicas**: Horizontal read scaling with multiple replica nodes
- ✅ **Automatic Failover**: Tools like Patroni, repmgr for automatic failover
- ✅ **Load Balancing**: PgBouncer, HAProxy for connection distribution
- ✅ **Split-brain Protection**: Built-in mechanisms to prevent data corruption

**Configuration Example:**
```yaml
# docker-compose.yml for PostgreSQL cluster
version: '3.8'
services:
  postgres-primary:
    image: postgres:15
    environment:
      POSTGRES_REPLICATION_USER: replicator
      POSTGRES_REPLICATION_PASSWORD: rep_password
    command: |
      postgres 
      -c wal_level=replica 
      -c max_wal_senders=3 
      -c max_replication_slots=3

  postgres-replica1:
    image: postgres:15
    environment:
      PGUSER: postgres
      POSTGRES_PASSWORD: password
    command: |
      bash -c "
      pg_basebackup -h postgres-primary -D /var/lib/postgresql/data -U replicator -v -P -W
      echo 'standby_mode = on' >> /var/lib/postgresql/data/recovery.conf
      echo 'primary_conninfo = ''host=postgres-primary port=5432 user=replicator''' >> /var/lib/postgresql/data/recovery.conf
      postgres
      "
    depends_on:
      - postgres-primary
```

#### **Automatic Failover with Patroni**
```yaml
# patroni.yml
name: postgresql-cluster-member1
scope: postgresql-cluster

restapi:
  listen: 0.0.0.0:8008
  connect_address: node1:8008

etcd:
  hosts: etcd1:2379,etcd2:2379,etcd3:2379

bootstrap:
  dcs:
    ttl: 30
    loop_wait: 10
    retry_timeout: 60
    maximum_lag_on_failover: 1048576
    postgresql:
      use_pg_rewind: true
      parameters:
        wal_level: replica
        hot_standby: "on"
        max_wal_senders: 10
        max_replication_slots: 10

postgresql:
  listen: 0.0.0.0:5432
  connect_address: node1:5432
  data_dir: /data/postgresql
  bin_dir: /usr/lib/postgresql/15/bin
  pgpass: /tmp/pgpass
  authentication:
    replication:
      username: replicator
      password: rep-pass
    superuser:
      username: postgres
      password: postgres-pass
```

### ✅ Valkey (Redis Fork) - EXCELLENT CLUSTER SUPPORT

#### **Redis-Compatible Clustering**
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Valkey Node 1 │◄──►│   Valkey Node 2 │◄──►│   Valkey Node 3 │
│   Master        │    │   Master        │    │   Master        │
│   Slots: 0-5461 │    │   Slots:5462-   │    │   Slots:10923-  │
│                 │    │        10922    │    │        16383    │
└─────────┬───────┘    └─────────┬───────┘    └─────────┬───────┘
          │                      │                      │
          ▼                      ▼                      ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Valkey Node 4 │    │   Valkey Node 5 │    │   Valkey Node 6 │
│   Replica       │    │   Replica       │    │   Replica       │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

**Supported Clustering Features:**
- ✅ **Native Clustering**: 16384 hash slots distributed across nodes
- ✅ **Automatic Failover**: Replicas automatically promote to masters
- ✅ **Horizontal Scaling**: Add/remove nodes without downtime
- ✅ **Partition Tolerance**: Continues operating during network splits
- ✅ **Redis Compatibility**: Drop-in replacement for Redis clusters

**Configuration Example:**
```yaml
# docker-compose.yml for Valkey cluster
version: '3.8'
services:
  valkey-node-1:
    image: valkey/valkey:latest
    command: valkey-server /usr/local/etc/valkey/valkey.conf --cluster-enabled yes --cluster-config-file nodes.conf --cluster-node-timeout 5000 --appendonly yes
    ports:
      - "7001:6379"
      - "17001:16379"

  valkey-node-2:
    image: valkey/valkey:latest
    command: valkey-server /usr/local/etc/valkey/valkey.conf --cluster-enabled yes --cluster-config-file nodes.conf --cluster-node-timeout 5000 --appendonly yes
    ports:
      - "7002:6379"
      - "17002:16379"

  valkey-node-3:
    image: valkey/valkey:latest
    command: valkey-server /usr/local/etc/valkey/valkey.conf --cluster-enabled yes --cluster-config-file nodes.conf --cluster-node-timeout 5000 --appendonly yes
    ports:
      - "7003:6379"
      - "17003:16379"
```

### ⚠️ Current Azure Table Storage - LIMITED CLUSTER SUPPORT

**Limitations:**
- ❌ **No Manual Clustering**: Clustering is handled entirely by Azure
- ❌ **No Failover Control**: Failover is automatic but not configurable
- ❌ **Vendor Lock-in**: Cannot run on-premises or other clouds
- ❌ **Limited Backup Control**: Cannot easily backup/restore to other environments

## Backup and Restore Analysis

### ✅ PostgreSQL - EXCELLENT BACKUP/RESTORE

#### **Multiple Backup Strategies**

**1. Point-in-Time Recovery (PITR)**
```bash
# Continuous archiving setup
archive_mode = on
archive_command = 'cp %p /backup/archive/%f'
wal_level = replica

# Base backup
pg_basebackup -h localhost -D /backup/base -U postgres -v -P -C

# Point-in-time restore
pg_ctl start -D /restore/data -o "-c config_file=/restore/postgresql.conf"
```

**2. Logical Backups with pg_dump**
```bash
# Full database backup
pg_dump -h localhost -U postgres -d orderdb > backup_$(date +%Y%m%d_%H%M%S).sql

# Schema-only backup
pg_dump -h localhost -U postgres -d orderdb --schema-only > schema_backup.sql

# Data-only backup
pg_dump -h localhost -U postgres -d orderdb --data-only > data_backup.sql

# Restore
psql -h localhost -U postgres -d orderdb < backup_20241208_143000.sql
```

**3. Physical Backups with pg_basebackup**
```bash
# Create base backup
pg_basebackup -h primary-server -D /backup/basebackup -U replication -v -P -W

# Automated backup script
#!/bin/bash
BACKUP_DIR="/backup/$(date +%Y/%m/%d)"
mkdir -p $BACKUP_DIR

pg_basebackup -h localhost -D $BACKUP_DIR/base -U postgres -v -P -C
pg_dump -h localhost -U postgres orderdb > $BACKUP_DIR/logical.sql

# Compress and upload to S3/Azure Storage
tar -czf $BACKUP_DIR.tar.gz $BACKUP_DIR
aws s3 cp $BACKUP_DIR.tar.gz s3://backups/postgresql/
```

#### **Marten-Specific Backup**
```csharp
// Event store backup using Marten
public class EventStoreBackupService
{
    private readonly IDocumentStore _store;
    
    public async Task BackupEventsAsync(DateTime fromDate, DateTime toDate)
    {
        using var session = _store.QuerySession();
        
        var events = await session.Events
            .QueryAllRawEvents()
            .Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate)
            .ToListAsync();
            
        var backupData = new EventBackup
        {
            Events = events,
            CreatedAt = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate
        };
        
        var json = JsonSerializer.Serialize(backupData);
        await File.WriteAllTextAsync($"events_backup_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.json", json);
    }
    
    public async Task RestoreEventsAsync(string backupFilePath)
    {
        var json = await File.ReadAllTextAsync(backupFilePath);
        var backupData = JsonSerializer.Deserialize<EventBackup>(json);
        
        using var session = _store.LightweightSession();
        
        foreach (var eventData in backupData.Events)
        {
            session.Events.Append(eventData.StreamId, eventData.Data);
        }
        
        await session.SaveChangesAsync();
    }
}
```

### ✅ Valkey - EXCELLENT BACKUP/RESTORE

#### **Redis-Compatible Backup Methods**

**1. RDB Snapshots**
```bash
# Manual snapshot
valkey-cli BGSAVE

# Automatic snapshots (valkey.conf)
save 900 1     # Save if at least 1 key changed in 900 seconds
save 300 10    # Save if at least 10 keys changed in 300 seconds
save 60 10000  # Save if at least 10000 keys changed in 60 seconds

# Copy RDB file for backup
cp /var/lib/valkey/dump.rdb /backup/dump_$(date +%Y%m%d_%H%M%S).rdb
```

**2. AOF (Append Only File)**
```bash
# Enable AOF (valkey.conf)
appendonly yes
appendfilename "appendonly.aof"
appendfsync everysec

# Rewrite AOF to optimize size
valkey-cli BGREWRITEAOF

# Backup AOF file
cp /var/lib/valkey/appendonly.aof /backup/appendonly_$(date +%Y%m%d_%H%M%S).aof
```

**3. Cluster Backup Script**
```bash
#!/bin/bash
# Backup entire Valkey cluster

CLUSTER_NODES=$(valkey-cli cluster nodes | cut -d' ' -f2 | cut -d'@' -f1)
BACKUP_DIR="/backup/valkey_cluster_$(date +%Y%m%d_%H%M%S)"
mkdir -p $BACKUP_DIR

for NODE in $CLUSTER_NODES; do
    HOST=$(echo $NODE | cut -d':' -f1)
    PORT=$(echo $NODE | cut -d':' -f2)
    
    echo "Backing up node $HOST:$PORT"
    valkey-cli -h $HOST -p $PORT BGSAVE
    
    # Wait for backup to complete
    while [ $(valkey-cli -h $HOST -p $PORT LASTSAVE) -eq $(valkey-cli -h $HOST -p $PORT LASTSAVE) ]; do
        sleep 1
    done
    
    # Copy RDB file
    scp $HOST:/var/lib/valkey/dump.rdb $BACKUP_DIR/dump_${HOST}_${PORT}.rdb
done

echo "Cluster backup completed in $BACKUP_DIR"
```

### ⚠️ Current Azure Table Storage - LIMITED BACKUP CONTROL

**Limitations:**
- ❌ **No Direct Backup API**: Cannot trigger immediate backups
- ❌ **Limited Point-in-Time**: Only available in premium tiers
- ❌ **No Cross-Region Backup**: Backup stays within same Azure region
- ❌ **Vendor-Specific Format**: Cannot easily migrate to other platforms

## Recommended Migration Plan

### Phase 1: Infrastructure Setup (Weeks 1-2)
```yaml
# Docker Compose for development cluster
version: '3.8'
services:
  postgres-primary:
    image: postgres:15
    environment:
      POSTGRES_DB: orderdb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_primary_data:/var/lib/postgresql/data
      - ./scripts/init-replication.sql:/docker-entrypoint-initdb.d/init-replication.sql
    command: >
      postgres 
      -c wal_level=replica 
      -c max_wal_senders=3 
      -c max_replication_slots=3
      -c archive_mode=on
      -c archive_command='cp %p /backup/archive/%f'

  postgres-replica:
    image: postgres:15
    environment:
      POSTGRES_DB: orderdb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_replica_data:/var/lib/postgresql/data
    depends_on:
      - postgres-primary

  valkey-node1:
    image: valkey/valkey:latest
    command: >
      valkey-server
      --cluster-enabled yes
      --cluster-config-file nodes.conf
      --cluster-node-timeout 5000
      --appendonly yes
    ports:
      - "7001:6379"

  valkey-node2:
    image: valkey/valkey:latest
    command: >
      valkey-server
      --cluster-enabled yes
      --cluster-config-file nodes.conf
      --cluster-node-timeout 5000
      --appendonly yes
    ports:
      - "7002:6379"

  valkey-node3:
    image: valkey/valkey:latest
    command: >
      valkey-server
      --cluster-enabled yes
      --cluster-config-file nodes.conf
      --cluster-node-timeout 5000
      --appendonly yes
    ports:
      - "7003:6379"

volumes:
  postgres_primary_data:
  postgres_replica_data:
```

### Phase 2: Marten Integration (Weeks 3-4)
```csharp
// Program.cs - Marten setup
services.AddMarten(options =>
{
    // Primary connection
    options.Connection(connectionString);
    
    // Multi-tenancy for services
    options.Policies.AllDocumentsAreMultiTenanted();
    
    // Event store configuration
    options.Events.DatabaseSchemaName = "events";
    options.Events.AppendMode = EventAppendMode.Rich;
    
    // Projections for read models
    options.Projections.Add<OrderSummaryProjection>(ProjectionLifecycle.Async);
    options.Projections.Add<CustomerSummaryProjection>(ProjectionLifecycle.Async);
    
    // Performance optimizations
    options.Events.UseIdentityMapForIdentifiers = true;
    options.Serializer(new SystemTextJsonSerializer());
})
.AddAsyncDaemon(DaemonMode.HotCold)
.OptimizeArtifactWorkflow();

// Configure Akka.NET with PostgreSQL
services.AddAkka("OrderSystem", (builder, sp) =>
{
    var connectionString = sp.GetService<IConfiguration>()
        .GetConnectionString("PostgreSQL");
        
    builder
        .WithJournaling("postgresql", 
            new PostgreSqlJournalOptions
            {
                ConnectionString = connectionString,
                SchemaName = "akka_journal",
                TableName = "event_journal",
                MetadataTableName = "journal_metadata"
            })
        .WithSnapshots("postgresql",
            new PostgreSqlSnapshotOptions
            {
                ConnectionString = connectionString,
                SchemaName = "akka_snapshots",
                TableName = "snapshot_store"
            });
});
```

### Phase 3: Backup Automation (Week 5)
```bash
#!/bin/bash
# comprehensive_backup.sh

BACKUP_BASE="/backup/$(date +%Y/%m/%d)"
mkdir -p $BACKUP_BASE

# PostgreSQL backup
echo "Starting PostgreSQL backup..."
pg_basebackup -h postgres-primary -D $BACKUP_BASE/postgres_base -U postgres -v -P -C
pg_dump -h postgres-primary -U postgres orderdb > $BACKUP_BASE/orderdb_logical.sql

# Valkey cluster backup
echo "Starting Valkey cluster backup..."
VALKEY_BACKUP_DIR="$BACKUP_BASE/valkey"
mkdir -p $VALKEY_BACKUP_DIR

valkey-cli -p 7001 BGSAVE
valkey-cli -p 7002 BGSAVE
valkey-cli -p 7003 BGSAVE

# Wait for backups to complete and copy files
sleep 10
docker cp valkey-node1:/data/dump.rdb $VALKEY_BACKUP_DIR/node1_dump.rdb
docker cp valkey-node2:/data/dump.rdb $VALKEY_BACKUP_DIR/node2_dump.rdb
docker cp valkey-node3:/data/dump.rdb $VALKEY_BACKUP_DIR/node3_dump.rdb

# Compress and upload
tar -czf $BACKUP_BASE.tar.gz $BACKUP_BASE
aws s3 cp $BACKUP_BASE.tar.gz s3://orderystem-backups/

echo "Backup completed: $BACKUP_BASE.tar.gz"

# Cleanup old backups (keep 30 days)
find /backup -name "*.tar.gz" -mtime +30 -delete
```

## Production Deployment Configuration

### Kubernetes Deployment
```yaml
# postgresql-cluster.yaml
apiVersion: postgresql.cnpg.io/v1
kind: Cluster
metadata:
  name: postgres-cluster
spec:
  instances: 3
  
  postgresql:
    parameters:
      max_connections: "200"
      shared_buffers: "256MB"
      effective_cache_size: "1GB"
      
  bootstrap:
    initdb:
      database: orderdb
      owner: orderuser
      secret:
        name: postgres-credentials
        
  storage:
    size: 100Gi
    storageClass: fast-ssd
    
  monitoring:
    enabled: true
    
  backup:
    target: "s3"
    s3:
      bucket: "orderystem-backups"
      region: "us-west-2"
    retentionPolicy: "30d"

---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: valkey-cluster
spec:
  serviceName: valkey-cluster
  replicas: 6
  selector:
    matchLabels:
      app: valkey
  template:
    metadata:
      labels:
        app: valkey
    spec:
      containers:
      - name: valkey
        image: valkey/valkey:latest
        command:
          - valkey-server
        args:
          - /etc/valkey/valkey.conf
          - --cluster-enabled
          - "yes"
          - --cluster-require-full-coverage
          - "no"
          - --cluster-node-timeout
          - "15000"
          - --cluster-config-file
          - /data/nodes.conf
          - --appendonly
          - "yes"
        ports:
        - containerPort: 6379
          name: client
        - containerPort: 16379
          name: gossip
        volumeMounts:
        - name: data
          mountPath: /data
        - name: config
          mountPath: /etc/valkey
  volumeClaimTemplates:
  - metadata:
      name: data
    spec:
      accessModes: ["ReadWriteOnce"]
      storageClassName: fast-ssd
      resources:
        requests:
          storage: 50Gi
```

## Summary

### ✅ Cluster Environment Support - EXCELLENT
- **PostgreSQL**: Native streaming replication, automatic failover with Patroni
- **Valkey**: Native clustering with 16384 hash slots, automatic failover
- **Akka.NET**: Full cluster sharding support with PostgreSQL persistence

### ✅ Failover Capabilities - EXCELLENT
- **PostgreSQL**: Automatic failover in <30 seconds with Patroni
- **Valkey**: Automatic master promotion, partition tolerance
- **Application**: Akka.NET cluster handles node failures gracefully

### ✅ Backup and Restore - EXCELLENT
- **PostgreSQL**: Point-in-time recovery, logical/physical backups, automated scripts
- **Valkey**: RDB snapshots, AOF logs, cluster-wide backup strategies
- **Cross-platform**: Standard backup formats work across all environments

### 🎯 Recommendations
1. **Migrate immediately** from Azure Table Storage to PostgreSQL + Marten
2. **Implement Valkey clustering** for high availability caching
3. **Set up automated backup pipelines** with cross-region replication
4. **Use Kubernetes operators** for production deployment (CloudNativePG, Valkey Operator)

The recommended stack provides enterprise-grade clustering, failover, and backup capabilities that far exceed the current Azure Table Storage limitations.