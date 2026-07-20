using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class OrderMappingExtensions
{
    public static OrderResponse ToDto(this Order order) =>
        new(
            order.Id,
            order.CustomerName,
            order.Email,
            order.Phone,
            order.Address,
            order.Latitude,
            order.Longitude,
            order.PaymentMethod.ToString(),
            order.Status.ToString(),
            order.Total,
            order.ShippingFee,
            order.ShippingZoneName,
            order.CreatedAt
        );

    public static OrderDetailResponse ToDetailDto(this Order order) =>
        new(
            order.Id,
            order.CustomerName,
            order.Email,
            order.Phone,
            order.Address,
            order.Latitude,
            order.Longitude,
            order.PaymentMethod.ToString(),
            order.Status.ToString(),
            order.Total,
            order.ShippingFee,
            order.ShippingZoneName,
            order.CreatedAt,
            order.PaymentConfirmedAt,
            order.Items.Select(i => new OrderItemResponse(
                i.Id,
                i.VariantId,
                i.Variant.Product.Name,
                i.Variant.Sku,
                i.Variant.Product.Images.OrderBy(img => img.SortOrder).FirstOrDefault()?.Url,
                i.Variant.OptionValues
                    .Select(ov => new CartItemOptionResponse(ov.OptionValue.ProductOption.Name, ov.OptionValue.Value))
                    .ToList(),
                i.Quantity,
                i.PriceAtPurchase
            )).ToList(),
            order.Notes
                .OrderBy(n => n.CreatedAt)
                .Select(n => new OrderNoteResponse(n.Id, n.Content, n.CreatedAt))
                .ToList(),
            order.QuickShipperOrderId,
            order.QuickShipperStatus,
            order.QuickShipperTrackingUrl,
            order.QuickShipperDeliveryFee
        );
}
