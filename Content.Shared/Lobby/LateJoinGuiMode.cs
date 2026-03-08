using Content.Shared.Roles;
using Content.Shared.Spawning;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Lobby;

/// <summary>
///     The system that originally created the custom spawn list </summary>
public enum LateJoinGuiMode : byte
{
    Default,
    CustomList,
}

//TODO:ERRANT This no longer needs to be in Shared?

/// <summary>
/// Data describing a custom late join option for the GUI
/// </summary>
/// <param name="Job">The prototype of the job that will be spawned</param>
/// <param name="Station">The station to spawn on</param>
/// <param name="Name">The name of the option on the GUI</param>
/// <param name="Desc">The tooltip of the option on the GUI</param>
/// <param name="Proto">A proper identifier for the spawn option. When a button is clicked, this will be sent back to the serverside system, which must be able to validate and handle this information.</param>
[Serializable, NetSerializable]
public record struct LateJoinCustomOption(
    ProtoId<JobPrototype> Job,
    NetEntity? Station,
    LocId Name,
    LocId Desc,
    string Proto);

//TODO:ERRANT this is kinda horrifying tbh
[Serializable, NetSerializable]
public record struct LateJoinCustomOptionWithOrigin(
    ProtoId<JobPrototype> Job,
    NetEntity? Station,
    LocId Name,
    LocId Desc,
    string Proto,
    LateJoinCustomListOrigin Origin);
