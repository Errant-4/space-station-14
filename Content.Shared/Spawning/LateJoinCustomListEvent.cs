using Content.Shared.GameTicking.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Spawning;

/// <summary>
/// Raised when the player clicks a button on a "Custom List" late join GUI
/// </summary>
/// <param name="job">The protoID of the job the player wants to spawn as</param>
/// <param name="station">The station the player wants to spawn on</param>
/// <param name="buttonId">The numeric ID of the button the player clicked, counted from 0. This is relevant to some custom systems.</param>
/// <param name="origin">This identifies the system that originally made this list, so that other custom handlers can ignore the event when it goes back to Server</param>
[Serializable, NetSerializable] //TODO:ERRANT LATER1 rename?
public sealed class LateJoinCustomListEvent(
    ProtoId<JobPrototype>? job,
    NetEntity station,
    ProtoId<SolitarySpawningPrototype> buttonId,
    LateJoinCustomListOrigin origin) : EntityEventArgs
{
    public string? Job = job;
    public NetEntity Station = station;
    public ProtoId<SolitarySpawningPrototype> ButtonId = buttonId;
    public LateJoinCustomListOrigin Origin = origin;
}

/// <summary>
///     The system that originally created the custom spawn list </summary>
public enum LateJoinCustomListOrigin : byte
{
    SolitarySpawningSystem,
}

