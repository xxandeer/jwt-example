using jwt_example.Data;
using jwt_example.Models;
using jwt_example.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace jwt_example.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase 
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly TokenService _tokenService;

    public UsersController(UserManager<User> userManager, ApplicationDbContext context, TokenService tokenService)
    {
        _userManager = userManager;
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register(RegistrationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userManager.CreateAsync(
            new User { UserName = request.Username, Email = request.Email, FirstName = request.FirstName, LastName = request.LastName },
            request.Password!
            );

        if (result.Succeeded)
        {
            request.Password = "";
            return CreatedAtAction(nameof(Register), new { email = request.Email }, request);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return BadRequest(ModelState);
    }

    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<AuthResponse>> Authenticate([FromBody] AuthRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var managedUser = await _userManager.FindByEmailAsync(request.Email!);

        if (managedUser == null)
        {
            return BadRequest("Bad credentials");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(managedUser, request.Password!);

        if (!isPasswordValid)
        {
            return BadRequest("Bad credentials");
        }

        var userInDb = _context.Users.FirstOrDefault(u => u.Email == request.Email);

        if (userInDb == null)
        {
            return BadRequest("Unauthorized");
        }

        var accessToken = _tokenService.CreateToken(userInDb);
        var refreshToken = _tokenService.CreateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            JwtToken = accessToken,
            Token = refreshToken,
            UserId = userInDb.Id,
            Expires = DateTime.UtcNow.AddDays(30),
            Created = DateTime.UtcNow,
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            Token = accessToken,
            RefreshToken = refreshToken
        });
    }


    [HttpPost]
    [Route("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var refreshToken = _context.RefreshTokens
    .AsEnumerable()
    .FirstOrDefault(rt => rt.Token == request.RefreshToken && rt.JwtToken == request.Token && !rt.IsExpired);

        if (refreshToken == null)
        {
            return Unauthorized("Invalid token");
        }

        var user = _context.Users
            .FirstOrDefault(u => u.Id == refreshToken.UserId);

        if (user == null)
        {
            return BadRequest("Unauthorized");
        }

        var newAccessToken = _tokenService.CreateToken(user);
        var newRefreshToken = _tokenService.CreateRefreshToken();

        var newRefreshTokenEntity = new RefreshToken
        {
            JwtToken = newAccessToken,
            Token = newRefreshToken,
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddDays(30),
            Created = DateTime.UtcNow,
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);
        _context.RefreshTokens.Remove(refreshToken);
        await _context.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }
}
