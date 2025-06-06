// -----------------------------------------------------------------------
// <copyright file="OrderItemTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Tests.Models
{
    using FluentAssertions;
    using OrderSystem.Contracts.Models;
    using Xunit;

    public class OrderItemTests
    {
        [Fact]
        public void OrderItem_ShouldCreateWithAllProperties()
        {
            // Arrange & Act
            var orderItem = new OrderItem("PROD123", "Test Product", 2, 15.99m);

            // Assert
            orderItem.ProductId.Should().Be("PROD123");
            orderItem.ProductName.Should().Be("Test Product");
            orderItem.Quantity.Should().Be(2);
            orderItem.UnitPrice.Should().Be(15.99m);
        }

        [Theory]
        [InlineData(1, 10.00, 10.00)]
        [InlineData(2, 15.50, 31.00)]
        [InlineData(0, 25.00, 0.00)]
        [InlineData(3, 9.99, 29.97)]
        public void TotalPrice_ShouldCalculateCorrectly(int quantity, decimal unitPrice, decimal expectedTotal)
        {
            // Arrange
            var orderItem = new OrderItem("PROD123", "Test Product", quantity, unitPrice);

            // Act
            var totalPrice = orderItem.TotalPrice;

            // Assert
            totalPrice.Should().Be(expectedTotal);
        }

        [Fact]
        public void OrderItem_RecordEquality_ShouldWork()
        {
            // Arrange
            var item1 = new OrderItem("PROD123", "Test Product", 2, 15.99m);
            var item2 = new OrderItem("PROD123", "Test Product", 2, 15.99m);
            var item3 = new OrderItem("PROD456", "Different Product", 2, 15.99m);

            // Assert
            item1.Should().Be(item2);
            item1.Should().NotBe(item3);
        }
    }
}