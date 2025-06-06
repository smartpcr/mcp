// -----------------------------------------------------------------------
// <copyright file="WeatherToolsTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MCP.Service.Tests.Tools
{
    using FluentAssertions;
    using MCP.Service.Tools;
    using Xunit;

    public class WeatherToolsTests
    {
        [Theory]
        [InlineData("New York")]
        [InlineData("90210")]
        [InlineData("London")]
        [InlineData("")]
        public void GetWeather_ShouldReturnFormattedWeatherString(string location)
        {
            // Act
            var result = WeatherTools.GetWeather(location);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain(location);
            result.Should().Contain("Current weather in");
            result.Should().Contain("Temperature:");
            result.Should().Contain("Conditions:");
        }

        [Fact]
        public void GetWeather_ShouldIncludeLocationInResponse()
        {
            // Arrange
            var location = "San Francisco";

            // Act
            var result = WeatherTools.GetWeather(location);

            // Assert
            result.Should().StartWith($"Current weather in {location}:");
        }

        [Fact]
        public void GetWeather_ShouldReturnConsistentFormat()
        {
            // Arrange
            var location = "Chicago";

            // Act
            var result = WeatherTools.GetWeather(location);

            // Assert
            result.Should().MatchRegex(@"Current weather in .+: Temperature: \d+°F, Conditions: .+");
        }
    }
}