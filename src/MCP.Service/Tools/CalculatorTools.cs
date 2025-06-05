// -----------------------------------------------------------------------
// <copyright file="CalculatorTools.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Tools
{
    using System;
    using MCPSharp;

    public class CalculatorTools
    {
        [McpTool("add", "Adds two numbers")]
        public static double Add(
            [McpParameter(true, "First number")] double a,
            [McpParameter(true, "Second number")] double b)
        {
            return a + b;
        }

        [McpTool("subtract", "Subtracts two numbers")]
        public static double Subtract(
            [McpParameter(true, "First number")] double a,
            [McpParameter(true, "Second number")] double b)
        {
            return a - b;
        }

        [McpTool("multiply", "Multiplies two numbers")]
        public static double Multiply(
            [McpParameter(true, "First number")] double a,
            [McpParameter(true, "Second number")] double b)
        {
            return a * b;
        }

        [McpTool("divide", "Divides two numbers")]
        public static double Divide(
            [McpParameter(true, "Dividend")] double a,
            [McpParameter(true, "Divisor")] double b)
        {
            if (b == 0)
            {
                throw new ArgumentException("Cannot divide by zero");
            }

            return a / b;
        }
    }
}