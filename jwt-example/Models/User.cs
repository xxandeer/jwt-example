using Microsoft.AspNetCore.Identity;

namespace jwt_example.Models;

public class User : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
