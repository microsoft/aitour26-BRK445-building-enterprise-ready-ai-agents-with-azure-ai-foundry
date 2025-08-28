namespace SharedEntities;

public class ProductInfo
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool InStock { get; set; }
    public bool IsAvailable { get; set; }
    public string Location { get; set; } = string.Empty;
    public int Aisle { get; set; }
    public string Section { get; set; } = string.Empty;
}