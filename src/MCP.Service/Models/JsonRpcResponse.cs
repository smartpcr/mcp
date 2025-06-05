// -----------------------------------------------------------------------
// <copyright file="JsonRpcResponse.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Models
{
    using Newtonsoft.Json;

    public class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")] public string JsonRpc => "2.0";
        [JsonProperty("id")] public object Id { get; set; }
        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)] public object Result { get; set; }
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)] public JsonRpcError Error { get; set; }
    }
}