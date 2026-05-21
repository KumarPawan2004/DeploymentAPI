using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DeploymentAPI.Data;
using DeploymentAPI.DTOs;
using DeploymentAPI.Models;

namespace DeploymentAPI.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public TestController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("run")]
    public async Task<IActionResult> RunTests()
    {
        var results = new System.Collections.Generic.List<string>();
        bool allPassed = true;

        void Log(string message, bool success = true)
        {
            results.Add($"{(success ? "✅" : "❌")} {message}");
            if (!success) allPassed = false;
        }

        try
        {
            Log("Starting Automated Integration Tests...");

            // Test 1: Self-Healing Admin Seed Check
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@noteshub.com");
            if (adminUser != null)
            {
                Log($"Test 1 Passed: Seed admin user found. Email: {adminUser.Email}, Role: {adminUser.Role}", adminUser.Role == "Admin");
            }
            else
            {
                Log("Test 1 Failed: Seed admin user not found in database.", false);
            }

            // Instantiate AuthController for functional testing
            var authController = new AuthController(_context, _configuration);

            // Test 2: Role Propagation during Signup (Register as Admin)
            string testEmail = $"testadmin-{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";
            var registerRequest = new RegisterDTO
            {
                FullName = "Test Admin User",
                Email = testEmail,
                Password = "testPassword123",
                Role = "Admin"
            };

            var registerResult = await authController.Register(registerRequest);
            if (registerResult.Result is OkObjectResult okRegister)
            {
                var registeredData = okRegister.Value as AuthResponseDTO;
                Log($"Test 2 Passed: Admin registration successful. Created User: {registeredData?.User?.FullName}, Assigned Role: {registeredData?.User?.Role}", registeredData?.User?.Role == "Admin");
            }
            else if (registerResult.Result is BadRequestObjectResult badRegister)
            {
                Log($"Test 2 Failed: Admin registration failed. Error: {badRegister.Value}", false);
            }
            else
            {
                Log("Test 2 Failed: Admin registration returned unknown result type.", false);
            }

            // Test 3: Successful Login with Correct Role
            var loginRequestCorrect = new LoginDTO
            {
                Email = testEmail,
                Password = "testPassword123",
                Role = "Admin"
            };

            var loginResultCorrect = await authController.Login(loginRequestCorrect);
            if (loginResultCorrect.Result is OkObjectResult okLogin)
            {
                var loggedInData = okLogin.Value as AuthResponseDTO;
                Log($"Test 3 Passed: Login with correct role ('Admin') succeeded. User Token generated: {!string.IsNullOrEmpty(loggedInData?.Token)}", !string.IsNullOrEmpty(loggedInData?.Token));
            }
            else if (loginResultCorrect.Result is BadRequestObjectResult badLogin)
            {
                Log($"Test 3 Failed: Login with correct role failed. Error: {badLogin.Value}", false);
            }
            else
            {
                Log("Test 3 Failed: Login with correct role returned unknown result.", false);
            }

            // Test 4: Blocked Login with Role Mismatch
            var loginRequestMismatch = new LoginDTO
            {
                Email = testEmail,
                Password = "testPassword123",
                Role = "User" // Database user has Admin, but we selected User
            };

            var loginResultMismatch = await authController.Login(loginRequestMismatch);
            if (loginResultMismatch.Result is BadRequestObjectResult okMismatch)
            {
                string errMsg = okMismatch.Value?.ToString() ?? "";
                bool expectedError = errMsg.Contains("The selected role does not match this account");
                Log($"Test 4 Passed: Login with role mismatch correctly rejected. Error Message: '{errMsg}'", expectedError);
            }
            else if (loginResultMismatch.Result is OkObjectResult)
            {
                Log("Test 4 Failed: Login with role mismatch was incorrectly accepted.", false);
            }
            else
            {
                Log($"Test 4 Failed: Login with role mismatch returned unknown response type.", false);
            }

            // Clean up the created test user so the database stays clean
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == testEmail);
            if (dbUser != null)
            {
                _context.Users.Remove(dbUser);
                await _context.SaveChangesAsync();
                Log("Test User Cleanup: Test user removed from database successfully.");
            }
        }
        catch (Exception ex)
        {
            Log($"Exception during tests: {ex.Message}\n{ex.StackTrace}", false);
        }

        var status = allPassed ? "ALL TESTS PASSED" : "TEST RUN FAILED";
        return Ok(new
        {
            Status = status,
            Summary = allPassed ? "Backend auth and role handling conforms to UI requirements." : "Some tests failed. Please inspect logs.",
            Results = results
        });
    }
}
