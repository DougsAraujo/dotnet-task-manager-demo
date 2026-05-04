using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InfoController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<object> Get() =>
        Ok(new
        {
            name = "Task Manager API",
            version = "1.0",
            documentation = "/swagger",
            message = "This endpoint is public and does not require authentication."
        });
}
