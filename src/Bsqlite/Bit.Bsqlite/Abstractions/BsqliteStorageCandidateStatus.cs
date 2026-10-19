namespace Bit.Bsqlite;

/// <summary>What happened to one candidate during storage selection.</summary>
public enum BsqliteStorageCandidateStatus
{
    /// <summary>Not examined, because an earlier candidate had already won.</summary>
    NotProbed,

    /// <summary>Named in the configuration, but no provider with that name is registered.</summary>
    NotRegistered,

    /// <summary>Probed and reported unusable in this browser.</summary>
    Unavailable,

    /// <summary>
    /// Usable, but rejected because it does not survive a reload and
    /// <see cref="BsqliteStorageSelection.AllowNonPersistentFallback"/> was not set.
    /// </summary>
    RejectedAsNonPersistent,

    /// <summary>Rejected because the database already exists on a different backend.</summary>
    RejectedByExistingBinding,

    /// <summary>Chosen.</summary>
    Selected,
}
