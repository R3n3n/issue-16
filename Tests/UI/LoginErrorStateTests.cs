using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Gateway.Tests
{
    // Write UI test: error states display correctly - MANZANO
    public class LoginErrorStateTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public LoginErrorStateTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task LoginPage_WithUnapprovedReturnUrl_ShowsErrorBanner()
        {
            var client = _factory.CreateClient();
            var html = await client.GetStringAsync("/Auth/Login?returnUrl=https://not-a-registered-app.example.com");

            Assert.Contains("Unapproved External App", html);
            Assert.Contains("sso-alert", html);
            Assert.Contains("role=\"alert\"", html);
        }

        [Fact]
        public async Task PostLogin_WithEmptyFields_RedisplaysFormWithValidationErrors()
        {
            var client = _factory.CreateClient();
            var getResponse = await client.GetAsync("/Auth/Login");
            var pageHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiForgeryToken(pageHtml);

            var formData = new Dictionary<string, string>
            {
                ["Email"] = "",
                ["Password"] = "",
                ["__RequestVerificationToken"] = token
            };

            var response = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(formData));
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("sso-field-error", html);
        }

        [Fact]
        public async Task PostLogin_WithInvalidEmailFormat_ShowsFieldError()
        {
            var client = _factory.CreateClient();
            var getResponse = await client.GetAsync("/Auth/Login");
            var pageHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiForgeryToken(pageHtml);

            var formData = new Dictionary<string, string>
            {
                ["Email"] = "not-an-email",
                ["Password"] = "somepassword",
                ["__RequestVerificationToken"] = token
            };

            var response = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(formData));
            var html = await response.Content.ReadAsStringAsync();

            Assert.Contains("valid email address", html, StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractAntiForgeryToken(string html)
        {
            const string marker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
            var start = html.IndexOf(marker, StringComparison.Ordinal);

            if (start == -1)
            {
                throw new InvalidOperationException("Antiforgery token not found on login page — check the form markup.");
            }

            start += marker.Length;
            var end = html.IndexOf('"', start);

            return html.Substring(start, end - start);
        }
    }
}