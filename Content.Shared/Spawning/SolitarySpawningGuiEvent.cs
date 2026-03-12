using Content.Shared.Lobby;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Spawning;

/// <summary>
/// Raised by the server when a system that provides custom latejoin options has updated the available spawn profiles.
/// These are then stored on the client to be shown when the player opens the Late Join UI
/// </summary>
[Serializable, NetSerializable]
public sealed class LateJoinGuiCustomButtonsEvent(List<LateJoinCustomOption> options, LateJoinCustomListOrigin origin) : EntityEventArgs
{
    public List<LateJoinCustomOption> Options = options;
    public LateJoinCustomListOrigin Origin = origin;
}
