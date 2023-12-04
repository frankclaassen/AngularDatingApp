using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class BuggyController : BaseApiController
{
  private readonly DataContext _context;

  public BuggyController(DataContext context)
  {
    _context = context;
  }

  [HttpGet("auth")]
  public ActionResult<string> GetSecret()
  {

    return "secret text";
  }

  [AllowAnonymous]
  [HttpGet("not-found")]
  public ActionResult<AppUser> GetNotFound()
  {
    var x = _context.Users.Find(-1);

    if (x == null) return NotFound();

    return x;
  }

  [AllowAnonymous]
  [HttpGet("server-error")]
  public ActionResult<string> GetServerError()
  {

    var x = _context.Users.Find(-1);

    return x.ToString();
  }

  [AllowAnonymous]
  [HttpGet("bad-request")]
  public ActionResult<string> GetBadRequest()
  {

    return BadRequest("bad request");
  }
}
