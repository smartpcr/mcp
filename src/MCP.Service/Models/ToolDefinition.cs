// -----------------------------------------------------------------------
// <copyright file="ToolDefinition.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class ToolDefinition
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("inputSchema")] public JObject InputSchema { get; set; }
    }
}