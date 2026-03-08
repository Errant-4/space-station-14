using Content.Client.Lobby;
using Content.Shared.Spawning;

namespace Content.Client.Spawning;

/// <summary>
/// This system receives Solitary Spawning profiles from the server and forwards them to Lobby handling
/// </summary>
/// <remarks>It literally only exists because a Manager can't subscribe</remarks>
//TODO:ERRANT should this be renamed to CustomLateJoin something something?
public sealed class SolitarySpawningSystem : EntitySystem //TODO:ERRANT Later2 this actually is not related to Solitary anymore, make it more generic
{
    [Dependency] private readonly ILobbyManager _lobby = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeNetworkEvent<LateJoinGuiCustomButtonsEvent>(OnGuiData);
    }

    // The client has received a Custom List GUI update from the server
    private void OnGuiData(LateJoinGuiCustomButtonsEvent args)
    {
        // The new information is sent to LobbyManager for local reference
        _lobby.UpdateCustomListGui(args);
    }
}
