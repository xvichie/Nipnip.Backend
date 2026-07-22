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

    // Set once a TBC checkout session has been created for this order (PaymentMethod.Tbc).
    // TBC's payId is an opaque string (e.g. "tpay-tbvqma2372015"), unlike Flitt's numeric id.
    public string? TbcPaymentId { get; set; }

    // TBC's async callback body only carries a payment id, with no generic passthrough field
    // like Flitt's merchant_data — so the cart session/referral context needed to finalize the
    // order (clear cart, record affiliate conversion) is stashed here at checkout time instead.
    public string? TbcMerchantData { get; set; }

    // Set once a Bank of Georgia pre-order checkout link has been created for this order
    // (PaymentMethod.Bog). BOG's callback body is signed but we don't hold their public key
    // to verify it, so — like Tbc — the callback is treated only as a "check now" trigger and
    // the real status is re-pulled with our own Bearer token; merchant data is stashed the
    // same way as Tbc's for the same reason (no reliable passthrough back from the gateway).
    public string? BogPreOrderId { get; set; }
    public string? BogMerchantData { get; set; }

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
