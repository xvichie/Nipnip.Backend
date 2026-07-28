using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class ProductOptionMappingExtensions
{
    public static string DisplayName(this ProductOption option) => option.NameKa ?? option.NameEn ?? option.NameRu ?? "";

    public static ProductOptionValueResponse ToDto(this ProductOptionValue value) =>
        new(value.Id, value.Value, value.ValueKa, value.ValueEn, value.ValueRu);

    public static ProductOptionResponse ToDto(this ProductOption option) =>
        new(option.Id, option.DisplayName(), option.NameKa, option.NameEn, option.NameRu, option.Values.Select(v => v.ToDto()).ToList());
}
