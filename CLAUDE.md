# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains two main implementations:

1. **POC MCP Service** (`src/poc/MCP.Service/`) - A .NET 9.0 MCP (Model Context Protocol) service using MCPSharp
2. **Actor-Based Order System** (`src/OrderSystem.*`) - A proof-of-concept order management system using Akka.NET

## Development Commands

### Build
```bash
dotnet build
```

### Run the POC MCP service
```bash
dotnet run --project src/poc/MCP.Service/MCP.Service.csproj
```

### Run the Order System API
```bash
dotnet run --project src/OrderSystem.Api/OrderSystem.Api.csproj
```

### Run tests
```bash
dotnet test
```

### Restore packages
```bash
dotnet restore
```

## Architecture

### POC MCP Service (`src/poc/MCP.Service/`)

Simple MCPSharp-based service with:
- Weather, calculator, and string manipulation tools
- Attribute-based tool definitions
- Automatic JSON-RPC handling

### Actor-Based Order System

#### Core Components (`src/OrderSystem.Core/`)

1. **Models** - Core domain models:
   - `OrderItem`, `Address`, `OrderStatus`, `PaymentStatus`

2. **Messages** - Command and Event definitions:
   - `OrderMessages.cs`: Order-related commands and events
   - `PaymentMessages.cs`: Payment processing messages
   - `CatalogMessages.cs`: Inventory management messages

3. **Actors** - Actor implementations:
   - `OrderActor`: Manages individual order lifecycle using event sourcing
   - Uses Akka.Persistence for state recovery

#### API Layer (`src/OrderSystem.Api/`)

- ASP.NET Core Web API with Akka.NET integration
- RESTful endpoints for order management
- Actor system hosting and supervision

## Key Implementation Details

### Actor Model Principles
- **Single-threaded execution**: Each actor processes one message at a time
- **Event sourcing**: All state changes persisted as events
- **Message-driven**: Communication through immutable messages
- **No distributed transactions**: Uses eventual consistency

### Order Flow
1. Create order → Check availability → Reserve items
2. Process payment → Create shipment → Update status
3. Compensation flows for failures (release reservations, cancel orders)

### Adding New Features

#### For MCP Service:
Add new tool classes in `src/poc/MCP.Service/Tools/` with `[McpTool]` attributes

#### For Order System:
1. Add new message types in `src/OrderSystem.Core/Messages/`
2. Create actor implementations in `src/OrderSystem.Core/Actors/`
3. Add API endpoints in `src/OrderSystem.Api/Controllers/`
4. Update actor supervision hierarchy as needed