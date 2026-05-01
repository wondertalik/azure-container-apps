namespace Libraries.Shared.Interfaces;

public interface IUpdatable
{
    DateTimeOffset UpdatedAt { get; set; }

    Guid UpdatedBy { get; set; }
}
