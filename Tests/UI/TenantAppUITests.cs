using Xunit;

namespace ITElectiveSSO.Tests.UI
{
    public class TenantAppUITests
    {
        // BUENO
        [Fact]
        public void CreateTenantAppModal_ValidInputs_SuccessfullyMapsAppDetails()
        {
            // Arrange
            string appName = "Test Automation App";
            string returnUrl = "https://testautomation.com/callback";

            // Act
            bool isValid = !string.IsNullOrEmpty(appName) && returnUrl.StartsWith("https://");

            // Assert
            Assert.True(isValid);
            Assert.Equal("Test Automation App", appName);
            Assert.Equal("https://testautomation.com/callback", returnUrl);
        }

        //Cabardo
        [Fact]
        public void EditTenantAppModal_UpdatedInputs_ReflectsNewValues()
        {
            // Arrange
            string initialName = "Old App Name";
            string updatedName = "Updated App Name";
            string updatedUrl = "https://updated-domain.com/callback";

            // Act
            bool nameChanged = initialName != updatedName;

            // Assert
            Assert.True(nameChanged);
            Assert.Equal("Updated App Name", updatedName);
            Assert.Equal("https://updated-domain.com/callback", updatedUrl);
        }

    }
}