using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Creators.DTOs;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Creators;

public class LinkTreeService(AppDbContext db)
{
    public async Task<LinkTreeResponse> GetBySlugAsync(string creatorSlug)
    {
        var creator = await db.Creators.FirstOrDefaultAsync(c => c.Slug == creatorSlug && c.IsActive)
            ?? throw new NotFoundException($"Creator '{creatorSlug}' not found.");

        return await BuildResponseAsync(creator);
    }

    public async Task<LinkTreeResponse> GetMeAsync(string clerkUserId)
    {
        var creator = await db.Creators.FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");

        return await BuildResponseAsync(creator);
    }

    private async Task<LinkTreeResponse> BuildResponseAsync(Creator creator)
    {
        var items = await db.LinkTreeItems
            .Where(i => i.CreatorId == creator.Id)
            .OrderBy(i => i.Position)
            .ToListAsync();

        var merchantIds = items.Select(i => i.MerchantId).ToList();
        var merchants = await db.Merchants
            .Where(m => merchantIds.Contains(m.Id) && m.IsActive)
            .ToDictionaryAsync(m => m.Id);

        var entries = items
            .Where(i => merchants.ContainsKey(i.MerchantId))
            .Select(i => new LinkTreeItemResponse(
                i.Id,
                i.MerchantId,
                merchants[i.MerchantId].Name,
                merchants[i.MerchantId].Slug,
                merchants[i.MerchantId].LogoUrl,
                i.Label,
                i.Position))
            .ToList();

        return new LinkTreeResponse(creator.Name, creator.Slug, creator.AvatarUrl, entries);
    }

    public async Task<LinkTreeItemResponse> AddItemAsync(string clerkUserId, AddLinkTreeItemRequest request)
    {
        var creator = await db.Creators.FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");

        var merchant = await db.Merchants.FindAsync(request.MerchantId)
            ?? throw new NotFoundException("Merchant not found.");

        if (await db.LinkTreeItems.AnyAsync(i => i.CreatorId == creator.Id && i.MerchantId == request.MerchantId))
            throw new ConflictException("This merchant is already on your link tree.");

        var maxPosition = await db.LinkTreeItems
            .Where(i => i.CreatorId == creator.Id)
            .Select(i => (int?)i.Position)
            .MaxAsync() ?? -1;

        var item = new LinkTreeItem
        {
            Id = Guid.NewGuid(),
            CreatorId = creator.Id,
            MerchantId = request.MerchantId,
            Label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim(),
            Position = maxPosition + 1,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.LinkTreeItems.Add(item);
        await db.SaveChangesAsync();

        return new LinkTreeItemResponse(item.Id, merchant.Id, merchant.Name, merchant.Slug, merchant.LogoUrl, item.Label, item.Position);
    }

    public async Task<LinkTreeItemResponse> UpdateItemAsync(string clerkUserId, Guid itemId, UpdateLinkTreeItemRequest request)
    {
        var item = await db.LinkTreeItems.FindAsync(itemId)
            ?? throw new NotFoundException("Link tree item not found.");

        var creator = await db.Creators.FindAsync(item.CreatorId);
        if (creator is null || creator.ClerkUserId != clerkUserId)
            throw new ForbiddenException("You can only edit your own link tree.");

        if (request.Label is not null)
            item.Label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim();

        await db.SaveChangesAsync();

        var merchant = await db.Merchants.FindAsync(item.MerchantId);
        return new LinkTreeItemResponse(
            item.Id, item.MerchantId, merchant?.Name ?? "", merchant?.Slug ?? "", merchant?.LogoUrl, item.Label, item.Position);
    }

    public async Task RemoveItemAsync(string clerkUserId, Guid itemId)
    {
        var item = await db.LinkTreeItems.FindAsync(itemId)
            ?? throw new NotFoundException("Link tree item not found.");

        var creator = await db.Creators.FindAsync(item.CreatorId);
        if (creator is null || creator.ClerkUserId != clerkUserId)
            throw new ForbiddenException("You can only edit your own link tree.");

        db.LinkTreeItems.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task ReorderAsync(string clerkUserId, ReorderLinkTreeItemsRequest request)
    {
        var creator = await db.Creators.FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");

        var items = await db.LinkTreeItems
            .Where(i => i.CreatorId == creator.Id)
            .ToListAsync();

        var itemsById = items.ToDictionary(i => i.Id);

        if (request.OrderedItemIds.Count != items.Count || !request.OrderedItemIds.All(itemsById.ContainsKey))
            throw new ArgumentException("Reorder list must include exactly the current link tree items.");

        for (var position = 0; position < request.OrderedItemIds.Count; position++)
            itemsById[request.OrderedItemIds[position]].Position = position;

        await db.SaveChangesAsync();
    }
}
