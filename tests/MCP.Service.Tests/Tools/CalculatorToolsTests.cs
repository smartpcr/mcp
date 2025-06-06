// -----------------------------------------------------------------------
// <copyright file="CalculatorToolsTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Tests.Tools
{
    using FluentAssertions;
    using MCP.Service.Tools;
    using Xunit;

    public class CalculatorToolsTests
    {
        [Theory]
        [InlineData(2, 3, 5)]
        [InlineData(-1, 1, 0)]
        [InlineData(0, 0, 0)]
        [InlineData(double.MaxValue, 0, double.MaxValue)]
        public void Add_ShouldReturnCorrectSum(double a, double b, double expected)
        {
            // Act
            var result = CalculatorTools.Add(a, b);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(5, 3, 2)]
        [InlineData(0, 5, -5)]
        [InlineData(-1, -1, 0)]
        [InlineData(double.MaxValue, 0, double.MaxValue)]
        public void Subtract_ShouldReturnCorrectDifference(double a, double b, double expected)
        {
            // Act
            var result = CalculatorTools.Subtract(a, b);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(2, 3, 6)]
        [InlineData(-2, 3, -6)]
        [InlineData(0, 5, 0)]
        [InlineData(1, 1, 1)]
        public void Multiply_ShouldReturnCorrectProduct(double a, double b, double expected)
        {
            // Act
            var result = CalculatorTools.Multiply(a, b);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(6, 2, 3)]
        [InlineData(5, 2, 2.5)]
        [InlineData(-6, 2, -3)]
        [InlineData(0, 5, 0)]
        public void Divide_ShouldReturnCorrectQuotient(double a, double b, double expected)
        {
            // Act
            var result = CalculatorTools.Divide(a, b);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void Divide_ByZero_ShouldThrowArgumentException()
        {
            // Act & Assert
            var action = () => CalculatorTools.Divide(5, 0);
            action.Should().Throw<ArgumentException>()
                .WithMessage("Cannot divide by zero");
        }
    }
}