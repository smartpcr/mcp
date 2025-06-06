// -----------------------------------------------------------------------
// <copyright file="PaymentMethodTests.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Contracts.Tests.Models
{
    using FluentAssertions;
    using OrderSystem.Contracts.Models;
    using Xunit;

    public class PaymentMethodTests
    {
        [Fact]
        public void PaymentMethod_ShouldCreateWithCreditCardType()
        {
            // Arrange & Act
            var paymentMethod = new PaymentMethod("CreditCard", "****1234", "12/25", "John Doe");

            // Assert
            paymentMethod.Type.Should().Be("CreditCard");
            paymentMethod.CardNumber.Should().Be("****1234");
            paymentMethod.ExpiryDate.Should().Be("12/25");
            paymentMethod.CardHolderName.Should().Be("John Doe");
        }

        [Fact]
        public void PaymentMethod_ShouldCreateWithBankAccount()
        {
            // Arrange & Act
            var paymentMethod = new PaymentMethod("BankTransfer", BankAccountNumber: "****5678");

            // Assert
            paymentMethod.Type.Should().Be("BankTransfer");
            paymentMethod.BankAccountNumber.Should().Be("****5678");
            paymentMethod.CardNumber.Should().BeNull();
        }

        [Fact]
        public void PaymentMethod_RecordEquality_ShouldWork()
        {
            // Arrange
            var method1 = new PaymentMethod("CreditCard", "****1234", "12/25", "John Doe");
            var method2 = new PaymentMethod("CreditCard", "****1234", "12/25", "John Doe");
            var method3 = new PaymentMethod("DebitCard", "****1234", "12/25", "John Doe");

            // Assert
            method1.Should().Be(method2);
            method1.Should().NotBe(method3);
        }

        [Theory]
        [InlineData("CreditCard")]
        [InlineData("DebitCard")]
        [InlineData("PayPal")]
        [InlineData("BankTransfer")]
        public void PaymentMethod_DifferentTypes_ShouldBeValid(string type)
        {
            // Arrange & Act
            var paymentMethod = new PaymentMethod(type);

            // Assert
            paymentMethod.Type.Should().Be(type);
        }
    }
}