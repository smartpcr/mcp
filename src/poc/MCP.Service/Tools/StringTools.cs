// -----------------------------------------------------------------------
// <copyright file="StringTools.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Tools
{
    using System;
    using System.ComponentModel;
    using ModelContextProtocol.Server;

    [McpServerToolType]
    public class StringTools
    {
        [McpServerTool, Description("Reverses a string")]
        public static string ReverseString(string input)
        {
            var charArray = input.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        [McpServerTool, Description("Converts a string to uppercase")]
        public static string ToUpperCase(string input)
        {
            return input.ToUpper();
        }

        [McpServerTool, Description("Converts a string to lowercase")]
        public static string ToLowerCase(string input)
        {
            return input.ToLower();
        }

        [McpServerTool, Description("Counts the number of words in a string")]
        public static int CountWords(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return 0;
            }

            return input.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}