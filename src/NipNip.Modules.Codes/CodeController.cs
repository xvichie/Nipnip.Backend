using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Codes.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Codes;

[ApiController]
[Route("api/codes")]
[Authorize]
public class CodeController(CodeService codeService) : ControllerBase
{
    [HttpGet("check")]
    [AllowAnonymous]
    public async Task<ActionResult<CheckCodeResponse>> Check(
        [FromQuery] string code,
        [FromQuery] Guid merchantId)
    {
        return Ok(await codeService.CheckAvailabilityAsync(code, merchantId));
    }

    [HttpPost]
    public async Task<ActionResult<CodeResponse>> Claim([FromBody] ClaimCodeRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await codeService.ClaimAsync(clerkUserId, request));
    }
}
