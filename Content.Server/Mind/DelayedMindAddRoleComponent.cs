using Robust.Shared.Prototypes;

namespace Content.Server.Mind;

/// <summary>
/// This is used to add mind roles to a currently-mindless entity.
/// The roles will be added when a mind is added to the entity.
/// </summary>
[RegisterComponent]
public sealed partial class DelayedMindAddRoleComponent : Component
{
    /// <summary>
    ///     Marks this Mind Role as Antagonist
    ///     A single antag Mind Role is enough to make the owner mind count as Antagonist.
    /// </summary>
    [DataField]
    public List<EntProtoId> Prototypes = new List<EntProtoId>();
}
