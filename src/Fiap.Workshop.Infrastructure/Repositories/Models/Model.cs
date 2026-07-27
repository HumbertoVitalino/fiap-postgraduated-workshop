namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public abstract class Model
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected Model() { }

    protected Model(
        Guid id,
        DateTime createdAt,
        DateTime? updatedAt = null
    )
    {
        Id = id;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
