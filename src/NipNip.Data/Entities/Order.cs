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
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaymentConfirmedAt { get; set; }

    public Store Store { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderNote> Notes { get; set; } = [];
}
