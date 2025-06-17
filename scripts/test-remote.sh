#!/bin/bash

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
SQLD_PORT=8080
SQLD_SERVICE="sqld"
TIMEOUT=30
SKIP_DOCKER=false

# Help function
show_help() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Test Nelknet.LibSQL against a local sqld server"
    echo ""
    echo "Options:"
    echo "  -h, --help      Show this help message"
    echo "  -p, --port      Port for sqld server (default: 8080)"
    echo "  -s, --service   Docker service name (default: sqld)"
    echo "  -t, --timeout   Timeout in seconds (default: 30)"
    echo "  --skip-docker   Don't start/stop Docker services"
    echo "  --unit-only     Run only unit tests (no integration tests)"
    echo ""
    echo "Examples:"
    echo "  $0                          # Run full test suite with Docker"
    echo "  $0 --skip-docker            # Run against existing server"
    echo "  $0 --port 8081 --service sqld-auth  # Use authenticated server"
    echo "  $0 --unit-only              # Run only unit tests"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -p|--port)
            SQLD_PORT="$2"
            shift 2
            ;;
        -s|--service)
            SQLD_SERVICE="$2"
            shift 2
            ;;
        -t|--timeout)
            TIMEOUT="$2"
            shift 2
            ;;
        --skip-docker)
            SKIP_DOCKER=true
            shift
            ;;
        --unit-only)
            echo -e "${BLUE}Running unit tests only...${NC}"
            dotnet test --filter "ClassName!=RemoteIntegrationTests"
            exit $?
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            show_help
            exit 1
            ;;
    esac
done

# Check if Docker is available
if ! command -v docker &> /dev/null && [ "$SKIP_DOCKER" = false ]; then
    echo -e "${RED}Docker is not available. Use --skip-docker to test against existing server.${NC}"
    exit 1
fi

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null && [ "$SKIP_DOCKER" = false ]; then
    echo -e "${RED}docker-compose is not available.${NC}"
    exit 1
fi

# Function to wait for server
wait_for_server() {
    local url="http://localhost:$SQLD_PORT/health"
    local count=0
    
    echo -e "${BLUE}Waiting for sqld server at $url...${NC}"
    
    while [ $count -lt $TIMEOUT ]; do
        if curl -s -f "$url" > /dev/null 2>&1; then
            echo -e "${GREEN}Server is ready!${NC}"
            return 0
        fi
        
        echo -n "."
        sleep 1
        count=$((count + 1))
    done
    
    echo ""
    echo -e "${RED}Server failed to start within $TIMEOUT seconds${NC}"
    return 1
}

# Function to cleanup
cleanup() {
    if [ "$SKIP_DOCKER" = false ]; then
        echo -e "${BLUE}Stopping Docker services...${NC}"
        docker-compose down
    fi
}

# Set up trap for cleanup
if [ "$SKIP_DOCKER" = false ]; then
    trap cleanup EXIT
fi

# Start Docker services if needed
if [ "$SKIP_DOCKER" = false ]; then
    echo -e "${BLUE}Starting $SQLD_SERVICE service...${NC}"
    docker-compose up -d "$SQLD_SERVICE"
    
    # Wait for server to be ready
    if ! wait_for_server; then
        echo -e "${RED}Failed to start sqld server${NC}"
        exit 1
    fi
else
    echo -e "${YELLOW}Skipping Docker startup, assuming server is running...${NC}"
    
    # Still check if server is available
    if ! curl -s -f "http://localhost:$SQLD_PORT/health" > /dev/null; then
        echo -e "${RED}Server at localhost:$SQLD_PORT is not available${NC}"
        exit 1
    fi
fi

# Set environment variables for integration tests
export LIBSQL_TEST_URL="http://localhost:$SQLD_PORT"
export LIBSQL_TEST_TOKEN="test-token"

echo -e "${GREEN}Environment configured:${NC}"
echo -e "  LIBSQL_TEST_URL: $LIBSQL_TEST_URL"
echo -e "  LIBSQL_TEST_TOKEN: $LIBSQL_TEST_TOKEN"
echo ""

# Run the tests
echo -e "${BLUE}Running all tests...${NC}"
if dotnet test --verbosity:normal; then
    echo -e "${GREEN}All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}Some tests failed!${NC}"
    exit 1
fi