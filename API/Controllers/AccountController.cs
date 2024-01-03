using API.DTO;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController : BaseApiController
{
  private readonly UserManager<AppUser> _userManager;
  private readonly ITokenService _tokenService;
  private readonly IMapper _mapper;

  public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMapper mapper)
  {
    _userManager = userManager;
    _tokenService = tokenService;
    _mapper = mapper;
  }

  [AllowAnonymous]
  [HttpPost("register")]
  public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
  {
    if (await UserExists(registerDto.Username))
    {
      return BadRequest("User already exists");
    }

    var user = _mapper.Map<AppUser>(registerDto);

    user.UserName = registerDto.Username.ToLower();

    var result = await _userManager.CreateAsync(user, registerDto.Password);

    if (!result.Succeeded) return BadRequest(result.Errors);

    var roleResults = await _userManager.AddToRoleAsync(user, "Member");

    if (!roleResults.Succeeded) return BadRequest(roleResults.Errors);

    return Ok(new UserDto
    {
      Username = user.UserName,
      KnownAs = user.KnownAs,
      Token = await _tokenService.CreateToken(user),
      Gender = user.Gender
    });
  }

  [AllowAnonymous]
  [HttpPost("login")]
  public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
  {
    var user = await _userManager.Users.Include(x => x.Photos).SingleOrDefaultAsync(u => u.UserName == loginDto.Username.ToLower());

    if (user == null) return Unauthorized("Invalid Credentials");

    var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);

    if (!result) return Unauthorized();

    return Ok(new UserDto
    {
      Username = user.UserName,
      KnownAs = user.KnownAs,
      Token = await _tokenService.CreateToken(user),
      PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
      Gender = user.Gender
    });
  }

  private async Task<bool> UserExists(string username)
  {
    return await _userManager.Users.AnyAsync(u => u.UserName == username.ToLower());
  }
}
