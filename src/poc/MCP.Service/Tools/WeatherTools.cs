// -----------------------------------------------------------------------
// <copyright file="WeatherTools.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Tools
{
    using System.ComponentModel;
    using ModelContextProtocol.Server;

    [McpServerToolType]
    public class WeatherTools
    {
        [McpServerTool, Description("Get current weather for a location")]
        public static string GetWeather(string location)
        {
            // TODO: Replace with real API call. Here we return dummy data:
            return $"Current weather in {location}: Temperature: 72°F, Conditions: Partly cloudy";
        }
    }
}