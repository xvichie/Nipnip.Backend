using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts.AiAgent;

public class KnowledgeBaseService(AppDbContext db, StoreService storeService, VoyageClient voyage)
{
    private const int TopK = 3;
    private const float MinRelevance = 0.3f;

    public async Task<List<KnowledgeBaseSectionResponse>> ListForOwnStoreAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var sections = await db.KnowledgeBaseSections
            .Where(s => s.StoreId == store.Id)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        // Self-healing backfill: sections carried over from the old fixed fields (or
        // created while Voyage was briefly unreachable) start with no embedding —
        // catch up here so search_knowledge_base never silently skips them for long.
        var unembedded = sections.Where(s => s.Embedding is null).ToList();
        if (unembedded.Count > 0)
        {
            var vectors = await voyage.EmbedAsync(unembedded.Select(s => s.Content).ToList(), "document");
            for (var i = 0; i < unembedded.Count; i++)
                unembedded[i].Embedding = vectors[i];

            await db.SaveChangesAsync();
        }

        return sections.Select(ToDto).ToList();
    }

    public async Task<KnowledgeBaseSectionResponse> CreateAsync(string clerkUserId, CreateKnowledgeBaseSectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("Content is required.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var content = request.Content.Trim();
        var vectors = await voyage.EmbedAsync([content], "document");

        var section = new KnowledgeBaseSection
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Title = request.Title.Trim(),
            Content = content,
            Embedding = vectors[0],
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        db.KnowledgeBaseSections.Add(section);
        await db.SaveChangesAsync();

        return ToDto(section);
    }

    public async Task<KnowledgeBaseSectionResponse> UpdateAsync(string clerkUserId, Guid sectionId, UpdateKnowledgeBaseSectionRequest request)
    {
        var section = await GetOwnSectionAsync(clerkUserId, sectionId);

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Title cannot be empty.");
            section.Title = request.Title.Trim();
        }

        if (request.Content is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ArgumentException("Content cannot be empty.");

            var trimmed = request.Content.Trim();
            if (trimmed != section.Content)
            {
                var vectors = await voyage.EmbedAsync([trimmed], "document");
                section.Embedding = vectors[0];
            }
            section.Content = trimmed;
        }

        section.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return ToDto(section);
    }

    public async Task DeleteAsync(string clerkUserId, Guid sectionId)
    {
        var section = await GetOwnSectionAsync(clerkUserId, sectionId);
        db.KnowledgeBaseSections.Remove(section);
        await db.SaveChangesAsync();
    }

    // Tool-facing — no clerk auth, called from the orchestrator with an already-resolved
    // Store, same pattern as AiAgentToolService's other methods.
    public async Task<List<(string Title, string Content)>> SearchAsync(Store store, string query)
    {
        var sections = await db.KnowledgeBaseSections
            .Where(s => s.StoreId == store.Id && s.Embedding != null)
            .ToListAsync();

        if (sections.Count == 0) return [];

        var queryVectors = await voyage.EmbedAsync([query], "query");
        var queryVector = queryVectors[0];

        return sections
            .Select(s => (Section: s, Score: CosineSimilarity(queryVector, s.Embedding!)))
            .Where(x => x.Score > MinRelevance)
            .OrderByDescending(x => x.Score)
            .Take(TopK)
            .Select(x => (x.Section.Title, x.Section.Content))
            .ToList();
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, magA = 0, magB = 0;
        var len = Math.Min(a.Length, b.Length);
        for (var i = 0; i < len; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return magA == 0 || magB == 0 ? 0 : dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
    }

    private async Task<KnowledgeBaseSection> GetOwnSectionAsync(string clerkUserId, Guid sectionId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return await db.KnowledgeBaseSections.FirstOrDefaultAsync(s => s.Id == sectionId && s.StoreId == store.Id)
            ?? throw new NotFoundException("Knowledge base section not found.");
    }

    private static KnowledgeBaseSectionResponse ToDto(KnowledgeBaseSection s) =>
        new(s.Id, s.Title, s.Content, s.UpdatedAt);
}
