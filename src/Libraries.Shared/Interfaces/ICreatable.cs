namespace Libraries.Shared.Interfaces;

public interface ICreatable
{
    DateTimeOffset CreatedAt { get; set; }

    Guid CreatedBy { get; set; }
}
