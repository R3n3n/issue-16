using System.IdentityModel.Tokens.Jwt;
using Gateway.Controllers;
using Gateway.Models;
using Gateway.Services;
using ITElectiveSSO.Models;
using ITELECTIVE_SSO.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests
{
    public class AuthControllerTests
    {
        private const string ReturnUrl = "https://salesapp.example.com/callback";
        private const string Email = "login@itelectivesso.local";
        private const string Password = "Password1";

        private static ServiceProvider BuildProvider(string dbName)
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "unit-test-secret-key-that-is-at-least-32-chars-long",
                    ["Jwt:Issuer"] = "IT_ELECTIVE_SSO",
                    ["Jwt:Audience"] = "IT_ELECTIVE_SSO_CLIENTS",
                    ["Jwt:ExpiryHours"] = "9"
                })
                .Build());

            services.AddDbContext<SsoDbContext>(o => o.UseInMemoryDatabase(dbName));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                // Must mirror Program.cs (Task 11)
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<SsoDbContext>()
            .AddDefaultTokenProviders();

            services.AddScoped<IReturnUrlValidator, ReturnUrlValidator>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddTransient<AuthController>();

            return services.BuildServiceProvider();
        }

        private static async Task SeedAsync(IServiceProvider provider, bool userIsActive = true)
        {
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = provider.GetRequiredService<SsoDbContext>();

            var app = new TenantApp { Name = "SalesApp", ReturnUrl = ReturnUrl, IsActive = true, CreatedAt = DateTime.UtcNow };
            context.TenantApps.Add(app);
            await context.SaveChangesAsync();

            var adminGroup = new Group { TenantAppId = app.Id, Name = "SalesApp-Admin", PowerLevel = 0, CreatedAt = DateTime.UtcNow };
            var managerGroup = new Group { TenantAppId = app.Id, Name = "SalesApp-Manager", PowerLevel = 1, CreatedAt = DateTime.UtcNow };
            context.Groups.AddRange(adminGroup, managerGroup);
            await context.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                EmailConfirmed = true,
                IsActive = userIsActive,
                CreatedAt = DateTime.UtcNow
            };
            await userManager.CreateAsync(user, Password);

            context.UserGroups.AddRange(
                new UserGroup { UserId = user.Id, GroupId = adminGroup.Id },
                new UserGroup { UserId = user.Id, GroupId = managerGroup.Id });
            await context.SaveChangesAsync();
        }

        private static IEnumerable<string> ErrorMessages(Controller controller) =>
            controller.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

        // Task 14
        [Fact]
        public async Task Login_Post_WithValidCredentials_RedirectsWithJwt()
        {
            var provider = BuildProvider(Guid.NewGuid().ToString());
            await SeedAsync(provider);
            var controller = provider.GetRequiredService<AuthController>();

            var result = await controller.Login(new LoginViewModel { Email = Email, Password = Password, ReturnUrl = ReturnUrl });

            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.StartsWith(ReturnUrl + "?token=", redirect.Url);

            var tokenString = redirect.Url.Substring((ReturnUrl + "?token=").Length);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

            var user = await provider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(Email);
            Assert.Equal(user!.Id, jwt.Claims.First(c => c.Type == "sub").Value);
            Assert.Equal(Email, jwt.Claims.First(c => c.Type == "email").Value);
            Assert.Equal("SalesApp", jwt.Claims.First(c => c.Type == "tenant_app").Value);

            var groups = jwt.Claims.First(c => c.Type == "groups").Value;
            Assert.Contains("SalesApp-Admin", groups);
            Assert.Contains("SalesApp-Manager", groups);
            Assert.Contains("SalesApp-Manager", jwt.Claims.First(c => c.Type == "levels").Value);

            Assert.True(jwt.ValidTo > DateTime.UtcNow.AddHours(8));   // 9-hour lifetime
        }

        // Task 15
        [Fact]
        public async Task Login_Post_WithWrongPassword_IsRejected()
        {
            var provider = BuildProvider(Guid.NewGuid().ToString());
            await SeedAsync(provider);
            var controller = provider.GetRequiredService<AuthController>();

            var result = await controller.Login(new LoginViewModel { Email = Email, Password = "WrongPassword1", ReturnUrl = ReturnUrl });

            Assert.IsType<ViewResult>(result);
            Assert.Contains("Invalid email or password.", ErrorMessages(controller));

            var context = provider.GetRequiredService<SsoDbContext>();
            Assert.Equal(1, await context.AuditLogs.CountAsync(a => a.Action == "LoginFailed"));
        }

        // Task 16 (also covers ISSUE 5: login rejects inactive users + "Account Suspended" message)
        [Fact]
        public async Task Login_Post_WithInactiveAccount_IsRejected()
        {
            var provider = BuildProvider(Guid.NewGuid().ToString());
            await SeedAsync(provider, userIsActive: false);
            var controller = provider.GetRequiredService<AuthController>();

            var result = await controller.Login(new LoginViewModel { Email = Email, Password = Password, ReturnUrl = ReturnUrl });

            Assert.IsType<ViewResult>(result);
            Assert.Contains("Account Suspended", ErrorMessages(controller));
        }

        // Task 17
        [Fact]
        public async Task Login_Post_WithUnregisteredReturnUrl_IsBlocked()
        {
            var provider = BuildProvider(Guid.NewGuid().ToString());
            await SeedAsync(provider);
            var controller = provider.GetRequiredService<AuthController>();

            var result = await controller.Login(new LoginViewModel { Email = Email, Password = Password, ReturnUrl = "https://evil.example.com/steal" });

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("UnapprovedApp", view.ViewName);

            var context = provider.GetRequiredService<SsoDbContext>();
            Assert.Equal(0, await context.AuditLogs.CountAsync(a => a.Action == "LoginSuccess"));
        }

        // Task 18
        [Fact]
        public async Task Login_Post_AfterFiveFailedAttempts_LocksAccountEvenWithCorrectPassword()
        {
            var provider = BuildProvider(Guid.NewGuid().ToString());
            await SeedAsync(provider);
            var controller = provider.GetRequiredService<AuthController>();

            for (int i = 0; i < 5; i++)
            {
                await controller.Login(new LoginViewModel { Email = Email, Password = "WrongPassword1", ReturnUrl = ReturnUrl });
                controller.ModelState.Clear();   // inside the loop: otherwise attempts 2-5 exit early and never reach the password check
            }

            var result = await controller.Login(new LoginViewModel { Email = Email, Password = Password, ReturnUrl = ReturnUrl });

            Assert.IsType<ViewResult>(result);   // a RedirectResult here would mean the lockout failed
            Assert.Contains("Too many failed login attempts. Please try again in 15 minutes.", ErrorMessages(controller));
        }
    }
}