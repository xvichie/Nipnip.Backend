using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Creators.DTOs;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Creators;

public class LinkTreeService(AppDbContext db)
{
    private static readonly Regex SlugRegex = new(@"^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled);
    private static readonly Regex NonAlphanumericRegex = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    // --- Public reads ---

    public async Task<PublicLinkTreeResponse> GetByTreeSlugAsync(string treeSlug)
    {
        var tree = await db.LinkTrees
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Slug == treeSlug)
            ?? throw new NotFoundException($"Link tree '{treeSlug}' not found.");

        return await BuildPublicResponseAsync(tree);
    }

    public async Task<PublicLinkTreeResponse> GetDefaultByCreatorSlugAsync(string creatorSlug)
    {
        var creator = await db.Creators.FirstOrDefaultAsync(c => c.Slug == creatorSlug && c.IsActive)
            ?? throw new NotFoundException($"Creator '{creatorSlug}' not found.");

        var tree = await db.LinkTrees
            .FirstOrDefaultAsync(t => t.CreatorId == creator.Id && t.IsDefault)
            ?? throw new NotFoundException($"Creator '{creatorSlug}' has no link tree yet.");

        return await BuildPublicResponseAsync(tree, creator);
    }

    private async Task<PublicLinkTreeResponse> BuildPublicResponseAsync(LinkTree tree, Creator? creator = null)
    {
        creator ??= await db.Creators.FindAsync(tree.CreatorId)
            ?? throw new NotFoundException("Creator not found.");

        var items = await BuildItemsAsync(tree.Id);
        return new PublicLinkTreeResponse(creator.Name, creator.Slug, creator.AvatarUrl, tree.Name, items);
    }

    // --- Self-service ---

    public async Task<List<LinkTreeSummaryResponse>> GetMyTreesAsync(string clerkUserId)
    {
        var creator = await db.Creators.FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");

        await EnsureDefaultTreeAsync(creator);

        var trees = await db.LinkTrees
            .Where(t => t.CreatorId == creator.Id)
            .OrderBy(t => t.Position)
            .ToListAsync();

        var itemCounts = await db.LinkTreeItems
            .Where(i => trees.Select(t => t.Id).Contains(i.LinkTreeId))
            .GroupBy(i => i.LinkTreeId)
            .Select(g => new { LinkTreeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LinkTreeId, x => x.Count);

        return trees
            .Select(t => new LinkTreeSummaryResponse(
                t.Id, t.Name, t.Slug, t.IsDefault, t.Position, itemCounts.GetValueOrDefault(t.Id, 0)))
            .ToList();
    }

    public async Task<LinkTreeDetailResponse> GetMyTreeDetailAsync(string clerkUserId, Guid treeId)
    {
        var tree = await GetOwnedTreeAsync(clerkUserId, treeId);
        var items = await BuildItemsAsync(tree.Id);
        return new LinkTreeDetailResponse(tree.Id, tree.Name, tree.Slug, tree.IsDefault, items);
    }

    public async Task<LinkTreeSummaryResponse> CreateTreeAsync(string clerkUserId, CreateLinkTreeRequest request)
    {
        var creator = await db.Creators.FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");

        ValidateName(request.Name);

        var maxPosition = await db.LinkTrees
            .Where(t => t.CreatorId == creator.Id)
            .Select(t => (int?)t.Position)
            .MaxAsync() ?? -1;

        var tree = new LinkTree
        {
            Id = Guid.NewGuid(),
            CreatorId = creator.Id,
            Name = request.Name.Trim(),
            Slug = await GenerateUniqueSlugAsync(request.Name),
            IsDefault = false,
            Position = maxPosition + 1,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.LinkTrees.Add(tree);
        await db.SaveChangesAsync();

        return new LinkTreeSummaryResponse(tree.Id, tree.Name, tree.Slug, tree.IsDefault, tree.Position, 0);
    }

    public async Task<LinkTreeSummaryResponse> UpdateTreeAsync(string clerkUserId, Guid treeId, UpdateLinkTreeRequest request)
    {
        var tree = await GetOwnedTreeAsync(clerkUserId, treeId);

        if (request.Name is not null)
        {
            ValidateName(request.Name);
            tree.Name = request.Name.Trim();
        }

        if (request.Slug is not null && request.Slug != tree.Slug)
        {
            ValidateSlug(request.Slug);
            if (await db.LinkTrees.AnyAsync(t => t.Slug == request.Slug && t.Id != tree.Id))
                throw new ConflictException($"Slug '{request.Slug}' is already taken.");
            tree.Slug = request.Slug;
        }

        await db.SaveChangesAsync();

        var itemCount = await db.LinkTreeItems.CountAsync(i => i.LinkTreeId == tree.Id);
        return new LinkTreeSummaryResponse(tree.Id, tree.Name, tree.Slug, tree.IsDefault, tree.Position, itemCount);
    }

    public async Task DeleteTreeAsync(string clerkUserId, Guid treeId)
    {
        var tree = await GetOwnedTreeAsync(clerkUserId, treeId);

        var siblingCount = await db.LinkTrees.CountAsync(t => t.CreatorId == tree.CreatorId);
        if (siblingCount <= 1)
            throw new ConflictException("You can't delete your only link tree.");

        db.LinkTrees.Remove(tree);

        if (tree.IsDefault)
        {
            var next = await db.LinkTrees
                .Where(t => t.CreatorId == tree.CreatorId && t.Id != tree.Id)
                .OrderBy(t => t.Position)
                .FirstAsync();
            next.IsDefault = true;
        }

        await db.SaveChangesAsync();
    }

    public async Task SetDefaultAsync(string clerkUserId, Guid treeId)
    {
        var tree = await GetOwnedTreeAsync(clerkUserId, treeId);

        var siblings = await db.LinkTrees
            .Where(t => t.CreatorId == tree.CreatorId && t.IsDefault && t.Id != tree.Id)
            .ToListAsync();
        foreach (var sibling in siblings) sibling.IsDefault = false;

        tree.IsDefault = true;
        await db.SaveChangesAsync();
    }

    public async Task<LinkTreeItemResponse> AddItemAsync(string clerkUserId, Guid treeId, AddLinkTreeItemRequest request)
    {
        var tree = await GetOwnedTreeAsync(clerkUserId, treeId);

        var merchant = await db.Merchants.FindAsync(request.MerchantId)
            ?? throw new NotFoundException("Merchant not found.");

        if (!merchant.IsPublic)
        {
            var isApproved = await db.MerchantApprovedCreators
                .AnyAsync(a => a.MerchantId == merchant.Id && a.CreatorId == tree.CreatorId);
            if (!isApproved)
                throw new ForbiddenException("This merchant is private and hasn't approved you yet.");
        }

        if (await db.LinkTreeItems.AnyAsync(i => i.LinkTreeId == tree.Id && i.MerchantId == request.MerchantId))
            throw new ConflictException("This merchant is already on this link tree.");

        var maxPosition = await db.LinkTreeItems
            .Where(i => i.LinkTreeId == tree.Id)
            .Select(i => (int?)i.Position)
            .MaxAsync() ?? -1;

        var item = new LinkTreeItem
        {
            Id = Guid.NewGuid(),
            LinkTreeId = tree.Id,
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

        await EnsureOwnsTreeAsync(clerkUserId, item.LinkTreeId);

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

        await EnsureOwnsTreeAsync(clerkUserId, item.LinkTreeId);

        db.LinkTreeItems.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task ReorderAsync(string clerkUserId, Guid treeId, ReorderLinkTreeItemsRequest request)
    {
        var tree = await GetOwnedTreeAsync(clerkUserId, treeId);

        var items = await db.LinkTreeItems
            .Where(i => i.LinkTreeId == tree.Id)
            .ToListAsync();

        var itemsById = items.ToDictionary(i => i.Id);

        if (request.OrderedItemIds.Count != items.Count || !request.OrderedItemIds.All(itemsById.ContainsKey))
            throw new ArgumentException("Reorder list must include exactly the current link tree items.");

        for (var position = 0; position < request.OrderedItemIds.Count; position++)
            itemsById[request.OrderedItemIds[position]].Position = position;

        await db.SaveChangesAsync();
    }

    // --- Helpers ---

    private async Task<LinkTree> GetOwnedTreeAsync(string clerkUserId, Guid treeId)
    {
        var tree = await db.LinkTrees.FindAsync(treeId)
            ?? throw new NotFoundException("Link tree not found.");

        var creator = await db.Creators.FindAsync(tree.CreatorId);
        if (creator is null || creator.ClerkUserId != clerkUserId)
            throw new ForbiddenException("You can only manage your own link trees.");

        return tree;
    }

    private async Task EnsureOwnsTreeAsync(string clerkUserId, Guid treeId)
    {
        await GetOwnedTreeAsync(clerkUserId, treeId);
    }

    private async Task EnsureDefaultTreeAsync(Creator creator)
    {
        if (await db.LinkTrees.AnyAsync(t => t.CreatorId == creator.Id)) return;

        var slug = creator.Slug;
        if (await db.LinkTrees.AnyAsync(t => t.Slug == slug))
            slug = $"{creator.Slug}-{Guid.NewGuid():N}"[..Math.Min(slug.Length + 9, 40)];

        db.LinkTrees.Add(new LinkTree
        {
            Id = Guid.NewGuid(),
            CreatorId = creator.Id,
            Name = "My Links",
            Slug = slug,
            IsDefault = true,
            Position = 0,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();
    }

    private async Task<List<LinkTreeItemResponse>> BuildItemsAsync(Guid treeId)
    {
        var items = await db.LinkTreeItems
            .Where(i => i.LinkTreeId == treeId)
            .OrderBy(i => i.Position)
            .ToListAsync();

        var merchantIds = items.Select(i => i.MerchantId).ToList();
        var merchants = await db.Merchants
            .Where(m => merchantIds.Contains(m.Id) && m.IsActive)
            .ToDictionaryAsync(m => m.Id);

        return items
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
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug) || !SlugRegex.IsMatch(slug))
            throw new ArgumentException("Slug must be lowercase alphanumeric with optional hyphens and cannot start with a hyphen.");
    }

    // Common informal Georgian->Latin transliteration (matches how Georgians typically romanize by
    // hand — e.g. პ/ფ both -> p, ტ/თ both -> t — not the strict reversible academic scheme).
    // Without this, Georgian names would just have every letter stripped as "non-alphanumeric".
    private static readonly Dictionary<char, string> GeorgianToLatin = new()
    {
        ['ა'] = "a", ['ბ'] = "b", ['გ'] = "g", ['დ'] = "d", ['ე'] = "e", ['ვ'] = "v", ['ზ'] = "z",
        ['თ'] = "t", ['ი'] = "i", ['კ'] = "k", ['ლ'] = "l", ['მ'] = "m", ['ნ'] = "n", ['ო'] = "o",
        ['პ'] = "p", ['ჟ'] = "zh", ['რ'] = "r", ['ს'] = "s", ['ტ'] = "t", ['უ'] = "u", ['ფ'] = "p",
        ['ქ'] = "k", ['ღ'] = "gh", ['ყ'] = "q", ['შ'] = "sh", ['ჩ'] = "ch", ['ც'] = "ts", ['ძ'] = "dz",
        ['წ'] = "ts", ['ჭ'] = "ch", ['ხ'] = "kh", ['ჯ'] = "j", ['ჰ'] = "h",
    };

    private static string Transliterate(string name)
    {
        var builder = new System.Text.StringBuilder(name.Length);
        foreach (var c in name)
            builder.Append(GeorgianToLatin.TryGetValue(c, out var latin) ? latin : c.ToString());
        return builder.ToString();
    }

    private static string Slugify(string name)
    {
        var slug = NonAlphanumericRegex.Replace(Transliterate(name).Trim().ToLowerInvariant(), "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "link-tree" : slug;
    }

    private async Task<string> GenerateUniqueSlugAsync(string name)
    {
        var baseSlug = Slugify(name);
        var slug = baseSlug;
        var suffix = 1;

        while (await db.LinkTrees.AnyAsync(t => t.Slug == slug))
            slug = $"{baseSlug}-{++suffix}";

        return slug;
    }
}
