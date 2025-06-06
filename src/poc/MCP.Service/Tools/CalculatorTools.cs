// -----------------------------------------------------------------------
// <copyright file="CalculatorTools.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Tools
{
    using System;
    using System.ComponentModel;
    using ModelContextProtocol.Server;

    [McpServerToolType]
    public class CalculatorTools
    {
        [McpServerTool, Description("Adds two numbers")]
        public static double Add(double a, double b)
        {
            return a + b;
        }

        [McpServerTool, Description("Subtracts two numbers")]
        public static double Subtract(double a, double b)
        {
            return a - b;
        }

        [McpServerTool, Description("Multiplies two numbers")]
        public static double Multiply(double a, double b)
        {
            return a * b;
        }

        [McpServerTool, Description("Divides two numbers")]
        public static double Divide(double a, double b)
        {
            if (b == 0)
            {
                throw new ArgumentException("Cannot divide by zero");
            }

            return a / b;
        }
    }
}