﻿namespace jwt_example.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string JwtToken { get; set; }
    public string Token { get; set; }
    public string UserId { get; set; }
    public DateTime Expires { get; set; }
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public DateTime Created { get; set; }
}
