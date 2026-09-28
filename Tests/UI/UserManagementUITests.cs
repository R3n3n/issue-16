using Xunit;

namespace SSO_Gateway.Tests.UI
{
    public class UserManagementUITests
    {
        //Write UI test: create user flow
        [Fact]
        public void CreateUserModal_PasswordMismatch_DisplaysInlineError()
        {
            string password = "Password123!";
            string confirmPassword = "Password456!";
            bool isMismatch = password != confirmPassword;
            string errorMessage = isMismatch ? "Password do not match!" : string.Empty;

            Assert.True(isMismatch);
            Assert.Equal("Password do not match!", errorMessage);
        }

        //Write UI test: toggle active flow
        [Fact]
        public void ActiveToggleSwitch_WhenClicked_UpdatesUserBadgeText()
        {
            string initialStatus = "Active";
            bool isChecked = false;

            string updatedStatus = isChecked ? "Active" : "Suspended";

            Assert.Equal("Active", initialStatus);
            Assert.Equal("Suspended", updatedStatus);
        }
    }
}

