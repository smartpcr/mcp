// -----------------------------------------------------------------------
// <copyright file="ToolListResult.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Models
{
    using Newtonsoft.Json;

    public class ToolListResult
    {
        [JsonProperty("tools")] public List<ToolDefinition> Tools { get; set; }
        [JsonProperty("nextCursor", NullValueHandling = NullValueHandling.Ignore)] public string NextCursor { get; set; }
    }
}