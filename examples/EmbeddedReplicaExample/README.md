# Embedded Replica Example

This example demonstrates how to use libSQL's embedded replica functionality, which allows you to create a local SQLite database that automatically syncs with a remote libSQL primary database.

## Prerequisites

1. A Turso account and database. Sign up at https://turso.tech
2. Your database URL and auth token from the Turso dashboard

## Running the Example

Set your environment variables:

```bash
export LIBSQL_PRIMARY_URL="libsql://your-database.turso.io"
export LIBSQL_AUTH_TOKEN="your-auth-token"
```

Then run:

```bash
dotnet run
```

## Features Demonstrated

### 1. Basic Embedded Replica
- Creating a local database that syncs with a remote primary
- Manual sync operations
- Working with local data

### 2. Automatic Sync
- Configuring automatic sync intervals
- Background synchronization
- Sync event handling

### 3. Offline Mode
- Working offline without network access
- Toggling between online and offline modes
- Queuing changes for later sync

### 4. Sync Events
- Monitoring sync operations
- Handling sync failures
- Tracking sync statistics

### 5. Read-Your-Writes Consistency
- Ensuring local writes are immediately visible
- Consistency guarantees for local operations

## Connection String Options

```csharp
// Basic embedded replica
"Data Source=local.db;SyncUrl=libsql://db.turso.io;AuthToken=token"

// With automatic sync every minute
"Data Source=local.db;SyncUrl=libsql://db.turso.io;AuthToken=token;SyncInterval=60000"

// With read-your-writes consistency
"Data Source=local.db;SyncUrl=libsql://db.turso.io;AuthToken=token;ReadYourWrites=true"

// Start in offline mode
"Data Source=local.db;SyncUrl=libsql://db.turso.io;AuthToken=token;Offline=true"
```

## Use Cases

Embedded replicas are ideal for:

1. **Edge Computing**: Run queries locally with periodic sync to cloud
2. **Offline-First Apps**: Work without internet, sync when connected
3. **Performance**: Local reads with eventual consistency
4. **Resilience**: Continue operating even if primary is unavailable
5. **Multi-Region**: Deploy replicas close to users for low latency

## Architecture

```
┌─────────────────┐     Sync      ┌──────────────────┐
│  Local SQLite   │ ←───────────→ │  Remote libSQL   │
│   (Embedded)    │               │    (Primary)     │
└─────────────────┘               └──────────────────┘
        ↑                                   ↑
        │                                   │
    Local R/W                          Remote R/W
        │                                   │
        ↓                                   ↓
┌─────────────────┐               ┌──────────────────┐
│ Your Local App  │               │  Other Clients   │
└─────────────────┘               └──────────────────┘
```

## Best Practices

1. **Initial Sync**: Always perform an initial sync when creating a new replica
2. **Error Handling**: Subscribe to SyncFailed events for production apps
3. **Sync Frequency**: Balance between data freshness and network usage
4. **Offline Handling**: Implement proper offline detection and recovery
5. **Conflict Resolution**: libSQL uses last-write-wins for conflicts