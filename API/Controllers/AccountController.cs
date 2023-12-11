using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTO;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController : BaseApiController
{
  private readonly DataContext _context;
  private readonly ITokenService _tokenService;
  private readonly IMapper _mapper;

  public AccountController(DataContext context, ITokenService tokenService, IMapper mapper)
  {
    _context = context;
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

    using var hmac = new HMACSHA512();

    user.UserName = registerDto.Username.ToLower();
    user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
    user.PasswordSalt = hmac.Key;

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    return Ok(new UserDto
    {
      Username = user.UserName,
      KnownAs = user.KnownAs,
      Token = _tokenService.CreateToken(user),
      Gender = user.Gender
    });
  }

  [AllowAnonymous]
  [HttpPost("login")]
  public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
  {
    var user = await _context.Users.Include(x => x.Photos).SingleOrDefaultAsync(u => u.UserName == loginDto.Username.ToLower());

    if (user == null) return Unauthorized("Invalid Credentials");

    using var hmac = new HMACSHA512(user.PasswordSalt);
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

    for (var i = 0; i < hash.Length; i++)
    {
      if (hash[i] != user.PasswordHash[i]) return Unauthorized("Invalid Credentials");
    }

    return Ok(new UserDto
    {
      Username = user.UserName,
      KnownAs = user.KnownAs,
      Token = _tokenService.CreateToken(user),
      PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
      Gender = user.Gender
    });
  }

  private async Task<bool> UserExists(string username)
  {
    return await _context.Users.AnyAsync(u => u.UserName == username.ToLower());
  }
}
