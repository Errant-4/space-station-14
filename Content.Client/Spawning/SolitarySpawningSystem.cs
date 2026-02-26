using Content.Client.Lobby;
using Content.Shared.Spawning;

namespace Content.Client.Spawning;

/// <summary>
/// This system receives Solitary Spawning profiles from the server and forwards them to Lobby handling
/// </summary>
/// <remarks>It literally only exists because a Manager can't subscribe</remarks>
public sealed class SolitarySpawningSystem : EntitySystem
{
    [Dependency] private readonly ILobbyManager _lobby = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {

        // SolitarySpawningStartedEvent
        SubscribeNetworkEvent<SolitarySpawningGuiDataEvent>(OnGuiData);
        // SolitarySpawningEndedEvent ?
    }

    private void OnGuiData(SolitarySpawningGuiDataEvent args)
    {
        //TODO:ERRANT Not sure what this actually does NOW, but it should merely switch the Lobby to Custom Gui mode
        // Clicking the Join button later should query the available options
        _lobby.RequestCustomListGui(args);
    }
}
