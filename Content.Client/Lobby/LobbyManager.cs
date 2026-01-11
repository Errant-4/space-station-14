using Content.Shared.Spawning;

namespace Content.Client.Lobby;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class LobbyManager : ILobbyManager
{
    // [Dependency] private readonly IPlayerManager _playerManager = default!;

    // public event Action<List<(ProtoId<JobPrototype>, NetEntity, LocId, LocId)>, LateJoinCustomListOrigin>? LateJoinCustomListReceived;
    public event Action<SolitarySpawningGuiDataEvent>? OnCustomListGuiRequest;

    public event Action? CloseJoinGui;

    public void Initialize() //TODO:ERRANT do I actually need this?
    {

    }

    /// <inheritdoc/>
    public void RequestCustomListGui(SolitarySpawningGuiDataEvent args)
    {
        OnCustomListGuiRequest?.Invoke(new SolitarySpawningGuiDataEvent(args.Options, args.Origin));
    }

    public void CloseAllLateJoinGui()
    {
        CloseJoinGui?.Invoke();
    }

}
