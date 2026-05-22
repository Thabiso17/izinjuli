using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

/// <summary>
/// All iDiski controllers inherit from this.
/// Provides ISender (MediatR) via property injection so every controller
/// stays a clean one-liner: return Ok(await Sender.Send(new MyQuery()));
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _sender;

    // We use 'Sender' here to satisfy the 23 existing controllers
    protected ISender Sender => 
        _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
