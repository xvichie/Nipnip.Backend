using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class CollectionMappingExtensions
{
    public static string DisplayName(this Collection collection) => collection.NameKa ?? collection.NameEn ?? collection.NameRu ?? "";

    public static CollectionResponse ToDto(this Collection collection) =>
        new(
            collection.Id,
            collection.DisplayName(),
            collection.NameKa,
            collection.NameEn,
            collection.NameRu,
            collection.Slug
        );
}
