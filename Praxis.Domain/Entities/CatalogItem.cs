namespace Praxis.Domain.Entities;

public class CatalogItem
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public decimal Price { get; set; }
}