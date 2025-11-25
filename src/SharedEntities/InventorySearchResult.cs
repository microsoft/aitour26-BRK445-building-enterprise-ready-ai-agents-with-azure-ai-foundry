namespace SharedEntities;
public class InventorySearchResult
{
    public ProductInfo[] ProductsFound { get; set; }
    public int TotalCount { get; set; }
    public string SearchQuery { get; set; }
}