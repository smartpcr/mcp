// -----------------------------------------------------------------------
// <copyright file="StringTools.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Tools
{
    using System;
    using MCPSharp;

    public class StringTools
    {
        [McpTool("reverse_string", "Reverses a string")]
        public static string ReverseString(
            [McpParameter(true, "String to reverse")] string input)
        {
            var charArray = input.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        [McpTool("to_uppercase", "Converts a string to uppercase")]
        public static string ToUpperCase(
            [McpParameter(true, "String to convert")] string input)
        {
            return input.ToUpper();
        }

        [McpTool("to_lowercase", "Converts a string to lowercase")]
        public static string ToLowerCase(
            [McpParameter(true, "String to convert")] string input)
        {
            return input.ToLower();
        }

        [McpTool("count_words", "Counts the number of words in a string")]
        public static int CountWords(
            [McpParameter(true, "String to analyze")] string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return 0;
            }

            return input.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}