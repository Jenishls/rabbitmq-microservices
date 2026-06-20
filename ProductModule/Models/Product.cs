namespace ProductModule.Models;

public class Product
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public int NumberUnits { get; set; }
    public List<string> Reviews { get; set; } = new();
    public List<string> MediaLinks { get; set; } = new();
    public string Color { get; set; } = string.Empty;
    public List<string> Sizes { get; set; } = new();
}
