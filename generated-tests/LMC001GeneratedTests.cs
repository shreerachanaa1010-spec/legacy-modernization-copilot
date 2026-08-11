using System;
using System.Threading.Tasks;
using Xunit;
using LegacySampleProject;

namespace LegacySampleProject.Tests
{
    public class CustomerServiceTests
    {
        [Fact]
        public void BadMethod_WhenCalled_ExecutesWithoutThrowing()
        {
            // Arrange
            using var customerService = new CustomerService();

            // Act
            var exception = Record.Exception(() => customerService.BadMethod());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task TestConfigureAwait_WhenCalled_CompletesSuccessfully()
        {
            // Arrange
            using var customerService = new CustomerService();

            // Act & Assert
            await customerService.TestConfigureAwait();
        }
    }
}