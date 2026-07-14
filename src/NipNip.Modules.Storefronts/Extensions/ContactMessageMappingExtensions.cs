using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class ContactMessageMappingExtensions
{
    public static ContactMessageResponse ToDto(this ContactMessage message) =>
        new(message.Id, message.Name, message.Email, message.Phone, message.Message, message.IsRead, message.CreatedAt);
}
