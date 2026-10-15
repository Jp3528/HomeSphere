using HomeSphere.Domain.Common;

namespace HomeSphere.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; } = 0;
    public string? Barcode { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AttributesJson { get; set; }
}
