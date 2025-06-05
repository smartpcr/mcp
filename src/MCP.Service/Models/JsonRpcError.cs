// -----------------------------------------------------------------------
// <copyright file="JsonRpcError.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Models
{
    using Newtonsoft.Json;

    public class JsonRpcError
    {
        [JsonProperty("code")] public int Code { get; set; }
        [JsonProperty("message")] public string Message { get; set; }
    }
}