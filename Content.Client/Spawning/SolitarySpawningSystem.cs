using Content.Client.Lobby;
using Content.Shared.Roles;
using Content.Shared.Spawning;
using Robust.Shared.Prototypes;

namespace Content.Client.Spawning;

/// <summary>
/// This system receives Solitary Spawning profiles from the server and forwards them to Lobby handling
/// </summary>
public sealed class SolitarySpawningSystem : EntitySystem
{
    [Dependency] private readonly ILobbyManager _lobby = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {


        SubscribeNetworkEvent<SolitarySpawningGuiDataEvent>(OnGuiData);
    }

    private void OnGuiData(SolitarySpawningGuiDataEvent args)
    {
        _lobby.RequestCustomListGui(args);
    }
}
