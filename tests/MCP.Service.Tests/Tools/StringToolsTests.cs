// -----------------------------------------------------------------------
// <copyright file="StringToolsTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Tests.Tools
{
    using FluentAssertions;
    using MCP.Service.Tools;
    using Xunit;

    public class StringToolsTests
    {
        [Theory]
        [InlineData("hello", "HELLO")]
        [InlineData("WORLD", "WORLD")]
        [InlineData("", "")]
        [InlineData("Mixed Case", "MIXED CASE")]
        public void ToUpperCase_ShouldReturnUppercaseString(string input, string expected)
        {
            // Act
            var result = StringTools.ToUpperCase(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("HELLO", "hello")]
        [InlineData("world", "world")]
        [InlineData("", "")]
        [InlineData("Mixed Case", "mixed case")]
        public void ToLowerCase_ShouldReturnLowercaseString(string input, string expected)
        {
            // Act
            var result = StringTools.ToLowerCase(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello", "olleh")]
        [InlineData("", "")]
        [InlineData("a", "a")]
        [InlineData("abc", "cba")]
        [InlineData("12345", "54321")]
        public void ReverseString_ShouldReturnReversedString(string input, string expected)
        {
            // Act
            var result = StringTools.ReverseString(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("hello world", 2)]
        [InlineData("", 0)]
        [InlineData("test string here", 3)]
        [InlineData("   ", 0)]
        [InlineData("single", 1)]
        public void CountWords_ShouldReturnCorrectWordCount(string input, int expected)
        {
            // Act
            var result = StringTools.CountWords(input);

            // Assert
            result.Should().Be(expected);
        }
    }
}