# Testing Nelknet.LibSQL

This document describes how to run the complete test suite, including integration tests against real libSQL servers.

## Quick Test Run

To run all unit tests (no server required):

```bash
dotnet test
```

## Integration Testing with sqld

### Option 1: Local sqld Server with Docker

1. **Start sqld server:**
   ```bash
   docker-compose up -d sqld
   ```

2. **Wait for server to be ready:**
   ```bash
   # Check health
   curl http://localhost:8080/health
   ```

3. **Run integration tests:**
   ```bash
   # Set environment variables for testing
   export LIBSQL_TEST_URL="http://localhost:8080"
   export LIBSQL_TEST_TOKEN="any-token-works-without-auth"
   
   # Run all tests including integration tests
   dotnet test
   ```

4. **Stop server:**
   ```bash
   docker-compose down
   ```

### Option 2: Local sqld with Authentication

1. **Generate JWT key:**
   ```bash
   # Generate a secure key
   JWT_KEY=$(openssl rand -base64 32)
   echo "JWT Key: $JWT_KEY"
   ```

2. **Update docker-compose.yml:**
   Edit the `sqld-auth` service and replace `your-secret-jwt-key-here-replace-with-secure-key` with your generated key.

3. **Start authenticated server:**
   ```bash
   docker-compose up -d sqld-auth
   ```

4. **Generate test token:**
   ```bash
   # Install jwt-cli if needed: cargo install jwt-cli
   # Or use online JWT generator with your key
   
   # Create a simple test token (replace with your JWT key)
   TEST_TOKEN=$(jwt encode --secret "$JWT_KEY" '{}')
   echo "Test Token: $TEST_TOKEN"
   ```

5. **Run integration tests:**
   ```bash
   export LIBSQL_TEST_URL="http://localhost:8081"
   export LIBSQL_TEST_TOKEN="$TEST_TOKEN"
   
   dotnet test
   ```

### Option 3: Turso Cloud Database

1. **Create Turso database:**
   ```bash
   # Install Turso CLI
   curl -sSfL https://get.tur.so/install.sh | bash
   
   # Login and create database
   turso auth login
   turso db create test-nelknet-libsql
   ```

2. **Get connection details:**
   ```bash
   # Get database URL
   turso db show test-nelknet-libsql --url
   
   # Create auth token
   turso db tokens create test-nelknet-libsql
   ```

3. **Run integration tests:**
   ```bash
   export LIBSQL_TEST_URL="libsql://your-database.turso.io"
   export LIBSQL_TEST_TOKEN="your-auth-token"
   
   dotnet test
   ```

4. **Clean up:**
   ```bash
   turso db destroy test-nelknet-libsql
   ```

## Test Categories

### Unit Tests
- **HttpConnectionTests**: HTTP client functionality, protocol implementation
- **LibSQLConnectionTests**: Connection string parsing, mode detection
- **LibSQLCommandTests**: Command execution, parameter binding
- **LibSQLDataReaderTests**: Data reading, type conversion

### Integration Tests
- **RemoteIntegrationTests**: Full end-to-end testing with real servers
  - Connection establishment
  - Query execution
  - Parameter binding
  - Transactions
  - Data type handling
  - Error scenarios

### Performance Tests
- **BulkInsertTests**: Large data operations
- **ConnectionPoolingTests**: Connection reuse patterns

## Test Configuration

### Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `LIBSQL_TEST_URL` | libSQL server URL | `http://localhost:8080` |
| `LIBSQL_TEST_TOKEN` | Authentication token | `eyJ0eXAiOiJKV1Q...` |

### Test Attributes

- `[Fact]`: Standard unit test
- `[Theory]`: Parameterized test with multiple inputs
- `[SkippableFact]`: Test that can be conditionally skipped

## Troubleshooting

### Connection Issues

1. **"Failed to connect to remote libSQL server"**
   - Ensure sqld server is running: `docker ps`
   - Check server health: `curl http://localhost:8080/health`
   - Verify environment variables are set

2. **Authentication errors**
   - Verify token is valid and not expired
   - Check JWT key matches between server and token generation
   - Ensure token format is correct (Bearer token)

3. **Network timeouts**
   - Check if server is accessible: `telnet localhost 8080`
   - Increase timeout in test configuration
   - Verify firewall/Docker network settings

### Server Issues

1. **sqld container won't start**
   ```bash
   # Check Docker logs
   docker-compose logs sqld
   
   # Ensure ports are available
   netstat -an | grep 8080
   ```

2. **Health check failures**
   ```bash
   # Check server status manually
   curl -v http://localhost:8080/health
   
   # Check server logs
   docker logs $(docker ps -q --filter "ancestor=ghcr.io/tursodatabase/libsql-server:latest")
   ```

### Test Failures

1. **"No current row available"**
   - Ensure test data exists
   - Check SQL syntax and execution order
   - Verify transaction isolation

2. **Type conversion errors**
   - Check libSQL type mapping
   - Verify parameter types match expected values
   - Review Hrana protocol implementation

## Continuous Integration

For CI/CD pipelines, use the Docker approach:

```yaml
# GitHub Actions example
services:
  sqld:
    image: ghcr.io/tursodatabase/libsql-server:latest
    ports:
      - 8080:8080
    env:
      SQLD_HTTP_LISTEN_ADDR: 0.0.0.0:8080
      SQLD_HTTP_CORS: true

env:
  LIBSQL_TEST_URL: http://localhost:8080
  LIBSQL_TEST_TOKEN: test-token
```

## Performance Testing

For performance analysis:

```bash
# Run with detailed timing
dotnet test --logger:console --verbosity:detailed

# Profile memory usage
dotnet test --collect:"XPlat Code Coverage"

# Benchmark specific operations
dotnet run --project tests/Nelknet.LibSQL.Benchmarks
```

## Contributing Tests

When adding new tests:

1. **Unit tests** for all new public APIs
2. **Integration tests** for new connection modes or protocols
3. **Error handling tests** for edge cases and failure scenarios
4. **Performance tests** for operations that might impact throughput

Follow the existing patterns and ensure tests are:
- **Isolated**: Don't depend on external state
- **Repeatable**: Same results every run
- **Fast**: Complete quickly for development workflow
- **Meaningful**: Test real-world scenarios