namespace EventManagement.Services;

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? UserId { get; set; }
    
    public static AuthResult Successful(int userId) => new() { Success = true, UserId = userId };
    public static AuthResult Failed(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
} 