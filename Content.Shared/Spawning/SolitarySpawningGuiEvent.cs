using Content.Shared.Lobby;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Spawning;

/// <summary>
/// Raised by the server when the player presses the late join button while a SolitarySpawning rule is active.
/// It sends the list of spawn options to the client
/// </summary>
[Serializable, NetSerializable] //TODO:ERRANT rename
public sealed class SolitarySpawningGuiDataEvent(List<(ProtoId<JobPrototype>, NetEntity?, LocId, LocId, string)> options, LateJoinCustomListOrigin origin) : EntityEventArgs
{
    public List<(ProtoId<JobPrototype>, NetEntity?, LocId, LocId, string)> Options = options;
    public LateJoinCustomListOrigin Origin = origin;
}

[Serializable, NetSerializable] //TODO:ERRANT move this elsewhere
public sealed class ChangeLateJoinGuiModeEvent(LateJoinGuiMode mode) : EntityEventArgs
{
    public LateJoinGuiMode Mode = mode;
}
