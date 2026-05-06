namespace Libraries.Shared.Interfaces;

public interface ICreatable
{
    DateTimeOffset CreatedAt { get; set; }

    string CreatedBy { get; set; }
}
