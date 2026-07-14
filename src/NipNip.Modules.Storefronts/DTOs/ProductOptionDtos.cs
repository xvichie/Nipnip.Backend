namespace NipNip.Modules.Storefronts.DTOs;

public record ProductOptionValueResponse(Guid Id, string Value);

public record ProductOptionResponse(Guid Id, string Name, List<ProductOptionValueResponse> Values);

public record CreateProductOptionRequest(string Name);

public record CreateProductOptionValueRequest(string Value);
