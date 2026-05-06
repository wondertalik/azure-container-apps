namespace Libraries.Shared.Interfaces;

public interface IUpdatable
{
    DateTimeOffset UpdatedAt { get; set; }

    string UpdatedBy { get; set; }
}
