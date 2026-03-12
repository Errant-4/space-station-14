using Content.Shared.Lobby;
using Content.Shared.Spawning;

namespace Content.Client.Lobby;

/// <summary>
/// This handles some data flow between the client's lobby UI and the server, that couldn't be done by LobbyState.
/// Possibly should replace LobbyState?
/// </summary>
public interface ILobbyManager
{
    /// <summary>
    ///     Opens a Custom late join GUI for the provided spawn options.
    /// </summary>
    void UpdateCustomListGui(LateJoinGuiCustomButtonsEvent args);

    /// <summary>
    ///     Opens a Custom late join GUI from the stored spawn options.
    /// </summary>
    List<LateJoinCustomOptionWithOrigin>  RequestCustomListGui();

    /// <summary>
    ///     Checks what join mode the Lobby is currently using.
    /// </summary>
    LateJoinGuiMode GetJoinMode();
}
