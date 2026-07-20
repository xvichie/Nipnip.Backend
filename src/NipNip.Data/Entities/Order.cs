using NipNip.Data.Enums;

namespace NipNip.Data.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string CustomerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public decimal ShippingFee { get; set; }
    public string? ShippingZoneName { get; set; }
    public OrderSource Source { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaymentConfirmedAt { get; set; }

    // Set once a QuickShipper delivery order has been created for this order.
    public int? QuickShipperOrderId { get; set; }
    public string? QuickShipperStatus { get; set; }
    public string? QuickShipperTrackingUrl { get; set; }
    public decimal? QuickShipperDeliveryFee { get; set; }

    // Set once a Flitt checkout session has been created for this order (PaymentMethod.Flitt).
    public long? FlittPaymentId { get; set; }

    // Snapshot of the originating Conversation's cumulative token usage at the moment
    // this order was drafted — only set for Source == OrderSource.AiAgent. Lets
    // cost-per-order be queried without joining back through conversation history.
    public long? AiInputTokens { get; set; }
    public long? AiOutputTokens { get; set; }
    public long? AiCacheReadInputTokens { get; set; }
    public long? AiCacheCreationInputTokens { get; set; }

    public Store Store { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderNote> Notes { get; set; } = [];
}
