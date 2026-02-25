using Content.Shared.Spawning;

namespace Content.Client.Lobby;

/// <summary>
/// This handles some data flow between the client's lobby UI and the server, that couldn't be done by LobbyState.
/// Possibly should replace LobbyState?
/// </summary>
public interface ILobbyManager
{
    /// <summary>
    ///     Fired when Gui data for a custom Late Join list is received.
    /// </summary>
    event Action<SolitarySpawningGuiDataEvent>? OnCustomListGuiRequest;

    /// <summary>
    ///     Instructs all Join GUIs to close
    /// </summary>
    event Action? CloseJoinGui;

    /// <summary>
    ///     Opens a Custom late join GUI for the provided spawn options.
    /// </summary>
    void RequestCustomListGui(SolitarySpawningGuiDataEvent args);

    /// <summary>
    ///     Closes all Late Join windows that are currently open.
    /// </summary>
    void CloseAllLateJoinGui();
}
