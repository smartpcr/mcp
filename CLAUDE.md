# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 9.0 MCP (Model Context Protocol) service implementation using the MCPSharp library. The service provides a standardized way for AI models to discover and invoke tools through JSON-RPC.

## Development Commands

### Build
```bash
dotnet build
```

### Run the service
```bash
dotnet run --project src/MCP.Service/MCP.Service.csproj
```

The service will start on port 5050 using MCPSharp's built-in server.

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

1. **MCPSharp Integration**
   - Uses MCPSharp library for all MCP protocol handling
   - Automatic JSON-RPC request/response management
   - Built-in parameter validation

2. **Tools** (`src/MCP.Service/Tools/`)
   - `WeatherTools.cs`: Contains weather-related MCP tools
   - Tools are defined using attributes:
     - `[McpTool]`: Marks a method as an MCP tool
     - `[McpParameter]`: Defines tool parameters

3. **Service Configuration**
   - Minimal setup using `MCPServer.StartAsync()`
   - Server name: "MCP Weather Service"
   - Version: "1.0.0"
   - Port: 5050

## Key Implementation Details

- Tools are discovered automatically via reflection
- MCPSharp handles all JSON-RPC communication
- Tools must be static methods decorated with `[McpTool]`
- Parameters use `[McpParameter]` for metadata

## Adding New Tools

To add a new tool:
1. Create a new class in the `Tools` folder or add to existing class
2. Add a static method with `[McpTool]` attribute
3. Use `[McpParameter]` on parameters to define requirements
4. Example:
   ```csharp
   [McpTool("tool_name", "Tool description")]
   public static string MyTool(
       [McpParameter(true, "Parameter description")] string param)
   {
       // Tool implementation
       return result;
   }
   ```