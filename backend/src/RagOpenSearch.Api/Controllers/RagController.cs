using Microsoft.AspNetCore.Mvc;
using RagOpenSearch.Application.DTOs;
using RagOpenSearch.Application.Interfaces;

namespace RagOpenSearch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RagController : ControllerBase
{
    private readonly IRagQueryService _ragQueryService;

    public RagController(IRagQueryService ragQueryService)
    {
        _ragQueryService = ragQueryService;
    }

    [HttpPost("search")]
    public async Task<ActionResult<SearchResponseDto>> Search(
        [FromBody] SearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _ragQueryService.SearchAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("ask")]
    public async Task<ActionResult<AskResponseDto>> Ask(
        [FromBody] AskRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _ragQueryService.AskAsync(request, cancellationToken);
        return Ok(result);
    }
}
