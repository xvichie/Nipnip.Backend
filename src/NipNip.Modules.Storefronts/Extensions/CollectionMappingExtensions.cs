using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class CollectionMappingExtensions
{
    public static CollectionResponse ToDto(this Collection collection) =>
        new(collection.Id, collection.Name, collection.Slug);
}
