using Content.Shared.Roles;
using Content.Shared.Spawning;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby;

/// <summary>
/// This is used for...
/// </summary>
public interface ILobbyManager
{


    /// <summary>
    ///     Fired when Gui data for a custom Late Join list is received.
    /// </summary>
    // event Action<OnCustomListReceived> OnCustomListReceived;
    event Action<SolitarySpawningGuiDataEvent>? OnCustomListGuiRequest;

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
