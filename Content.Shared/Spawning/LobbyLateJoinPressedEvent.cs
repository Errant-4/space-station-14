using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Spawning;

[Serializable, NetSerializable] //TODO:ERRANT rename
public sealed class LobbyLateJoinButtonPressedEvent() : HandledEntityEventArgs;
