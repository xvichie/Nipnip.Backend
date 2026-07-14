namespace NipNip.Modules.Storefronts.DTOs;

public record ProductImageResponse(Guid Id, string Url, int SortOrder);

public record CreateProductImageRequest(string Url);

public record ReorderProductImagesRequest(List<Guid> ImageIds);
