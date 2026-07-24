namespace NipNip.Modules.Storefronts.DTOs;

public record CollectionResponse(Guid Id, string Name, string Slug);

public record CreateCollectionRequest(string Name);

public record UpdateCollectionRequest(string? Name);

/// <summary>Full ordered replace of a collection's product membership — mirrors SetRelatedProductsRequest.</summary>
public record SetCollectionProductsRequest(List<Guid> ProductIds);
