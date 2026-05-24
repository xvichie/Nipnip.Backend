using NipNip.Data.Entities;
using NipNip.Modules.Creators.DTOs;

namespace NipNip.Modules.Creators.Extensions;

public static class CreatorMappingExtensions
{
    public static CreatorResponse ToDto(this Creator creator) =>
        new(
            creator.Id,
            creator.Name,
            creator.Slug,
            creator.AvatarUrl,
            creator.InstagramHandle,
            creator.InstagramFollowers,
            creator.TiktokHandle,
            creator.TiktokFollowers,
            creator.YoutubeHandle,
            creator.YoutubeFollowers,
            creator.FacebookHandle,
            creator.FacebookFollowers,
            creator.XHandle,
            creator.XFollowers,
            creator.LinkedinHandle,
            creator.LinkedinFollowers,
            creator.IsActive,
            creator.IsHighlighted,
            creator.CreatedAt
        );
}
