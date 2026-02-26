using Robust.Shared.Serialization;

namespace Content.Shared.Spawning;

/// <summary>
/// Event raised from the client, when the player presses the late Join button
/// </summary>
/// <remarks>
/// This can be used by specific systems to handle special spawning situations.
/// </remarks>
[Serializable, NetSerializable]
public sealed class LobbyLateJoinButtonPressedEvent : EntityEventArgs;
