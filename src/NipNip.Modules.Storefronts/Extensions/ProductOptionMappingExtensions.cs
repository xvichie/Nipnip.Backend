using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class ProductOptionMappingExtensions
{
    public static ProductOptionValueResponse ToDto(this ProductOptionValue value) =>
        new(value.Id, value.Value);

    public static ProductOptionResponse ToDto(this ProductOption option) =>
        new(option.Id, option.Name, option.Values.Select(v => v.ToDto()).ToList());
}
