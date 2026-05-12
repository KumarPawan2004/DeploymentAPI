namespace DeploymentAPI.Models.DTOs;  // Changed from backend.Models.DTOs

public class RegisterDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}