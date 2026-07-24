namespace NipNip.Modules.Storefronts.DTOs;

public record ProductImportRowResult(int RowNumber, string Name, string Action, string? Message);

public record ProductImportResult(int Created, int Updated, int Skipped, List<ProductImportRowResult> Rows);
