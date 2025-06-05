// -----------------------------------------------------------------------
// <copyright file="ToolsController.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Controllers
{
    using MCP.Service.Models;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;

    [ApiController]
    [Route("rpc")]
    public class ToolsController : ControllerBase
    {
        // In-memory list of available tools (could be loaded from config or a registry)
        private static readonly List<ToolDefinition> _tools = new()
        {
            new ToolDefinition
            {
                Name = "get_weather",
                Description = "Get current weather for a location",
                InputSchema = JObject.Parse(@"{
                  ""type"": ""object"",
                  ""properties"": {
                    ""location"": { ""type"": ""string"", ""description"": ""City or zip code"" }
                  },
                  ""required"": [""location""]
                }")
            }
            // Add more ToolDefinition instances here
        };

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonRpcRequest request)
        {
            var response = new JsonRpcResponse { Id = request.Id };

            switch (request.Method)
            {
                case "tools/list":
                    response.Result = HandleToolsList(request.Params);
                    break;

                case "tools/call":
                    response.Result = await HandleToolsCall(request.Params);
                    break;

                default:
                    response.Error = new JsonRpcError { Code = -32601, Message = $"Method not found: {request.Method}" };
                    break;
            }

            return Ok(response);
        }

        private ToolListResult HandleToolsList(JToken parameters)
        {
            // For simplicity, ignore pagination (cursor). Return all tools.
            return new ToolListResult
            {
                Tools = _tools,
                NextCursor = null
            };
        }

        private async Task<ToolCallResult> HandleToolsCall(JToken parameters)
        {
            var name = parameters["name"]?.Value<string>();
            var args = parameters["arguments"] as JObject;

            if (string.IsNullOrEmpty(name))
            {
                return new ToolCallResult
                {
                    Content = new List<object> { new { type = "text", text = "Missing tool name" } },
                    IsError = true
                };
            }

            var tool = _tools.FirstOrDefault(t => t.Name == name);
            if (tool == null)
            {
                return new ToolCallResult
                {
                    Content = new List<object> { new { type = "text", text = $"Unknown tool: {name}" } },
                    IsError = true
                };
            }

            // Example: implement get_weather
            if (name == "get_weather")
            {
                var location = args?["location"]?.Value<string>();
                if (string.IsNullOrEmpty(location))
                {
                    return new ToolCallResult
                    {
                        Content = new List<object> { new { type = "text", text = "Error: 'location' is required" } },
                        IsError = true
                    };
                }

                // TODO: Replace with real API call. Here we return dummy data:
                var weatherText = $"Current weather in {location}: Temperature: 72°F, Conditions: Partly cloudy";
                return new ToolCallResult
                {
                    Content = new List<object> { new { type = "text", text = weatherText } },
                    IsError = false
                };
            }

            // Default fallback (should not occur if tools are registered properly)
            return new ToolCallResult
            {
                Content = new List<object> { new { type = "text", text = $"Tool '{name}' not implemented" } },
                IsError = true
            };
        }
    }
}