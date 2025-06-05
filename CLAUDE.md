# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 9.0 MCP (Model Context Protocol) service implementation that provides a JSON-RPC API for tool listing and invocation. The service exposes tools that can be called by AI models through a standardized protocol.

## Development Commands

### Build
```bash
dotnet build
```

### Run the service
```bash
dotnet run --project src/MCP.Service/MCP.Service.csproj
```

The service will start on `http://0.0.0.0:5050` (hardcoded in Program.cs:7).

### Run tests
```bash
dotnet test
```

### Restore packages
```bash
dotnet restore
```

## Architecture

### Core Components

1. **ToolsController** (`src/MCP.Service/Controllers/ToolsController.cs`)
   - Single controller handling all JSON-RPC requests at `/rpc` endpoint
   - Implements two methods:
     - `tools/list`: Returns available tools with their schemas
     - `tools/call`: Executes a specific tool with provided arguments
   - Currently includes a sample `get_weather` tool implementation

2. **Models** (`src/MCP.Service/Models/`)
   - `JsonRpcRequest/Response`: Standard JSON-RPC 2.0 message format
   - `ToolDefinition`: Tool metadata including name, description, and JSON Schema for parameters
   - `ToolListResult/ToolCallResult`: Response formats for tool operations
   - `JsonRpcError`: Error response structure

3. **Service Configuration**
   - Minimal ASP.NET Core setup with controller routing
   - No authentication/authorization configured
   - Logging configured via standard appsettings.json

## Key Implementation Details

- Tools are statically defined in `_tools` list in ToolsController
- Tool execution logic is implemented directly in the controller (see `get_weather` example)
- Response content follows MCP format with `type: "text"` content blocks
- Error handling returns appropriate JSON-RPC error codes

## Adding New Tools

To add a new tool:
1. Add a `ToolDefinition` to the `_tools` list in ToolsController
2. Implement the tool's execution logic in `HandleToolsCall` method
3. Follow the existing pattern for parameter validation and response formatting