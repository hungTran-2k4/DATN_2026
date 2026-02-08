namespace MyProject.Domain.Entities.Stores;

public class Publisher
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Website { get; set; }
    //public string? HeadquartersLocation { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<Game>? Games { get; set; }
}