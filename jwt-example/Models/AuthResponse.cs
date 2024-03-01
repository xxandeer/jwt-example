namespace jwt_example.Models;

public class AuthResponse
{
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
}
