using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using Xunit;

namespace Gateway.Tests
{
    // Write UI test: login flow - CABARDO
    public class LoginFlowTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public LoginFlowTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task LoginPage_Returns200_AndRendersExpectedTitle()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/Auth/Login");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("IT ELECTIVE", html);
            Assert.Contains("SIGN IN", html);
        }

        [Fact]
        public async Task LoginPage_ContainsEmailAndPasswordFields()
        {
            var client = _factory.CreateClient();
            var html = await client.GetStringAsync("/Auth/Login");

            Assert.Contains("id=\"Email\"", html);
            Assert.Contains("id=\"Password\"", html);
            Assert.Contains("type=\"submit\"", html);
        }

        [Fact]
        public async Task LoginPage_DoesNotContainRegistrationLink()
        {
            var client = _factory.CreateClient();
            var html = await client.GetStringAsync("/Auth/Login");

            Assert.DoesNotContain("Register", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Sign up", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LoginPage_FieldsHaveAccessibleLabelsAndAriaAttributes()
        {
            var client = _factory.CreateClient();
            var html = await client.GetStringAsync("/Auth/Login");

            Assert.Contains("for=\"Email\"", html);
            Assert.Contains("for=\"Password\"", html);
            Assert.Contains("aria-required=\"true\"", html);
            Assert.Contains("aria-describedby=\"email-error\"", html);
            Assert.Contains("aria-describedby=\"password-error\"", html);
        }

        [Fact]
        public async Task LoginPage_WithAppReturnUrl_DisplaysAppName()
        {
            var client = _factory.CreateClient();
            var html = await client.GetStringAsync("/Auth/Login?returnUrl=https://sales.example.com/callback");

            Assert.Contains("Signing in to", html);
        }
    }
}
