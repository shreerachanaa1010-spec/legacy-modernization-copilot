using Xunit;
using LegacySampleProject;

namespace LegacySampleProject.Tests
{
    public class LMC001GeneratedTests
    {
        [Fact]
        public void BadMethod_CurrentBehavior_CompletesSuccessfully()
        {
            // Arrange
            var customerService = new CustomerService();

            // Act
            customerService.BadMethod();

            // Assert
            Assert.True(true);
        }
    }
}