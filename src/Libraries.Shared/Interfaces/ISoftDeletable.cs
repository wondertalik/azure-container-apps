namespace Libraries.Shared.Interfaces;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }

    string? DeletedBy { get; set; }
}
