// -----------------------------------------------------------------------
// <copyright file="JsonRpcRequest.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Models
{
    using Newtonsoft.Json;

    public class JsonRpcRequest
    {
        [JsonProperty("jsonrpc")] public string JsonRpc { get; set; }
        [JsonProperty("id")] public object Id { get; set; }
        [JsonProperty("method")] public string Method { get; set; }
        [JsonProperty("params")] public Newtonsoft.Json.Linq.JToken Params { get; set; }
    }
}