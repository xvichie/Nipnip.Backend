using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts.AiAgent;

[ApiController]
[Route("api/stores/me/ai-agent/knowledge-base")]
[Authorize]
public class KnowledgeBaseController(KnowledgeBaseService knowledgeBase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<KnowledgeBaseSectionResponse>>> GetAll()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await knowledgeBase.ListForOwnStoreAsync(clerkUserId));
    }

    [HttpPost]
    public async Task<ActionResult<KnowledgeBaseSectionResponse>> Create([FromBody] CreateKnowledgeBaseSectionRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await knowledgeBase.CreateAsync(clerkUserId, request));
    }

    [HttpPut("{sectionId:guid}")]
    public async Task<ActionResult<KnowledgeBaseSectionResponse>> Update(Guid sectionId, [FromBody] UpdateKnowledgeBaseSectionRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await knowledgeBase.UpdateAsync(clerkUserId, sectionId, request));
    }

    [HttpDelete("{sectionId:guid}")]
    public async Task<IActionResult> Delete(Guid sectionId)
    {
        var clerkUserId = User.GetClerkUserId();
        await knowledgeBase.DeleteAsync(clerkUserId, sectionId);
        return NoContent();
    }
}
