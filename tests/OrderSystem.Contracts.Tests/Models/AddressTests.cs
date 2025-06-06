// -----------------------------------------------------------------------
// <copyright file="AddressTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Tests.Models
{
    using FluentAssertions;
    using OrderSystem.Contracts.Models;
    using Xunit;

    public class AddressTests
    {
        [Fact]
        public void Address_ShouldCreateWithAllProperties()
        {
            // Arrange & Act
            var address = new Address("123 Main St", "Anytown", "CA", "12345", "USA");

            // Assert
            address.Street.Should().Be("123 Main St");
            address.City.Should().Be("Anytown");
            address.State.Should().Be("CA");
            address.ZipCode.Should().Be("12345");
            address.Country.Should().Be("USA");
        }

        [Fact]
        public void Address_ShouldHaveDefaultCountry()
        {
            // Arrange & Act
            var address = new Address("123 Main St", "Anytown", "CA", "12345");

            // Assert
            address.Country.Should().Be("US");
        }

        [Fact]
        public void Address_RecordEquality_ShouldWork()
        {
            // Arrange
            var address1 = new Address("123 Main St", "Anytown", "CA", "12345");
            var address2 = new Address("123 Main St", "Anytown", "CA", "12345");
            var address3 = new Address("456 Oak Ave", "Anytown", "CA", "12345");

            // Assert
            address1.Should().Be(address2);
            address1.Should().NotBe(address3);
        }
    }
}