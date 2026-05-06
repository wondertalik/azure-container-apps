namespace Libraries.Shared.Interfaces;

public interface ILockable
{
    DateTimeOffset? LockedAt { get; set; }
}
