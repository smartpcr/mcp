// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service
{
    using System;
    using MCPSharp;

    internal class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure and start the MCP server
            var serverName = "MCP Weather Service";
            var serverVersion = "1.0.0";

            // Set the port via environment variable or use default
            Environment.SetEnvironmentVariable("MCP_PORT", "5050");

            // Start the MCP server
            await MCPServer.StartAsync(serverName, serverVersion);
        }
    }
}