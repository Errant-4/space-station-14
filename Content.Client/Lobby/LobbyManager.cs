using Content.Shared.Spawning;

namespace Content.Client.Lobby;

public sealed partial class LobbyManager : ILobbyManager
{
    public event Action<SolitarySpawningGuiDataEvent>? OnCustomListGuiRequest;

    public event Action? CloseJoinGui;

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
