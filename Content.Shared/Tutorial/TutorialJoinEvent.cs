using Robust.Shared.Serialization;

namespace Content.Shared.Tutorial;

// [ByRefEvent]
// public record struct TutorialJoinEvent(ICommonSession Player, int OptionSelected);


[Serializable, NetSerializable] //TODO:ERRANT rename
public sealed class TutorialJoinEvent(string? job, NetEntity station, int optionSelected) : EntityEventArgs
{
    public string? Job = job;
    public NetEntity Station = station;
    public int OptionSelected = optionSelected;
}
