namespace NipNip.Modules.Storefronts.DTOs;

public record KnowledgeBaseSectionResponse(
    Guid Id,
    string Title,
    string Content,
    DateTimeOffset UpdatedAt);

public record CreateKnowledgeBaseSectionRequest(string Title, string Content);

public record UpdateKnowledgeBaseSectionRequest(string? Title, string? Content);
