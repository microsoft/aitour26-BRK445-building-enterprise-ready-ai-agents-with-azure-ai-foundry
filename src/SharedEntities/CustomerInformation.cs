namespace SharedEntities;

public class CustomerInformation
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string[] OwnedTools { get; set; } = Array.Empty<string>();
    public string[] Skills { get; set; } = Array.Empty<string>();
}
