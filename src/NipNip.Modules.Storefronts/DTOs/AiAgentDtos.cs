namespace NipNip.Modules.Storefronts.DTOs;

public record AiAgentSettingsResponse(
    bool EnabledFacebook,
    bool EnabledInstagram,
    string? Instructions);

public record UpdateAiAgentSettingsRequest(
    bool? EnabledFacebook,
    bool? EnabledInstagram,
    string? Instructions);

public record ConversationMessageResponse(
    Guid Id,
    string Direction,
    string Content,
    DateTimeOffset CreatedAt);

public record ConversationSummaryResponse(
    Guid Id,
    string? CustomerDisplayName,
    string ExternalUserId,
    DateTimeOffset LastMessageAt,
    DateTimeOffset CreatedAt);

public record ConversationDetailResponse(
    Guid Id,
    string? CustomerDisplayName,
    string ExternalUserId,
    DateTimeOffset LastMessageAt,
    DateTimeOffset CreatedAt,
    List<ConversationMessageResponse> Messages);

// Temporary — exercises the full orchestrator loop with a synthetic PSID, no Facebook
// involved. Remove (or gate further) once the real Messenger webhook is wired up.
public record AiAgentDebugMessageRequest(string? Psid, string Text);

public record AiAgentDebugMessageResponse(string Reply);
