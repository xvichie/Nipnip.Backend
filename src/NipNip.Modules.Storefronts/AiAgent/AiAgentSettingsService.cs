using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Crypto;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts.AiAgent;

public class AiAgentSettingsService(AppDbContext db, StoreService storeService, GraphApiClient graph, AesStringProtector protector)
{
    public async Task<AiAgentSettingsResponse> GetAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return ToDto(store);
    }

    public async Task<AiAgentSettingsResponse> UpdateAsync(string clerkUserId, UpdateAiAgentSettingsRequest request)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        if (request.Instructions is not null)
            store.AiAgentInstructions = string.IsNullOrWhiteSpace(request.Instructions) ? null : request.Instructions.Trim();

        // Only re-subscribe on the false->true transition, not on every settings save.
        if (request.EnabledFacebook is true && !store.AiAgentEnabledFacebook)
        {
            if (store.FacebookPageId is null || store.FacebookPageAccessTokenEncrypted is null)
                throw new ConflictException("Connect your Facebook Page first.");

            await graph.SubscribePageToMessagingAsync(store.FacebookPageId, protector.Decrypt(store.FacebookPageAccessTokenEncrypted));
        }

        if (request.EnabledFacebook.HasValue)
            store.AiAgentEnabledFacebook = request.EnabledFacebook.Value;

        // Instagram DM handling isn't wired up yet — this just persists the preference
        // for when it launches, no Graph subscribe call to make.
        if (request.EnabledInstagram.HasValue)
            store.AiAgentEnabledInstagram = request.EnabledInstagram.Value;

        await db.SaveChangesAsync();
        return ToDto(store);
    }

    private static AiAgentSettingsResponse ToDto(Store store) => new(
        store.AiAgentEnabledFacebook,
        store.AiAgentEnabledInstagram,
        store.AiAgentInstructions);
}
