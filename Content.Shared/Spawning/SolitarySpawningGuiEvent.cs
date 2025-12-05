using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Spawning;

/// <summary>
/// Raised by the server when the player presses the late join button while a SolitarySpawning rule is active.
/// It sends the list of spawn options to the client
/// </summary>
[Serializable, NetSerializable] //TODO:ERRANT rename
public sealed class SolitarySpawningGuiDataEvent(List<(ProtoId<JobPrototype>, NetEntity?, LocId, LocId, string)> options, LateJoinCustomListOrigin origin) : HandledEntityEventArgs
{
    public List<(ProtoId<JobPrototype>, NetEntity?, LocId, LocId, string)> Options = options;
    public LateJoinCustomListOrigin Origin = origin;
}
