// -----------------------------------------------------------------------
// <copyright file="ToolCallResult.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Models
{
    using Newtonsoft.Json;

    public class ToolCallResult
    {
        [JsonProperty("content")] public List<object> Content { get; set; }
        [JsonProperty("isError")] public bool IsError { get; set; }
    }
}