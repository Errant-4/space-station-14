using System.Linq;
using Content.Shared.Lobby;
using Content.Shared.Roles;
using Content.Shared.Spawning;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby;

public sealed partial class LobbyManager : ILobbyManager
{
    public event Action<LateJoinGuiCustomButtonsEvent>? OnCustomListGuiRequest;

    // public event Action? CloseJoinGui;

    public Dictionary<LateJoinCustomListOrigin, List<LateJoinCustomOption>> StoredOptions = new();

    public LateJoinGuiMode LateJoinMode = LateJoinGuiMode.Default;

    public void UpdateCustomListGui(LateJoinGuiCustomButtonsEvent args)
    {


        // Origin is identical among all entries in a particular received batch, so using the first one is fine
        // var origin = args.Options.First().Origin;

        StoredOptions.Remove(args.Origin);

        if (args.Options.Count > 0)
        {
            StoredOptions.Add(args.Origin, args.Options);
        }

        // TODO update lobby joinmode?
    }


    public List<LateJoinCustomOptionWithOrigin> RequestCustomListGui()
    {
        var result = new List<LateJoinCustomOptionWithOrigin>();

        foreach (var (origin, options) in StoredOptions)
        {
            foreach (var option in options)
            {
                var d = new LateJoinCustomOptionWithOrigin( //TODO:ERRANT I don't like this
                    option.Job,
                    option.Station,
                    option.Name,
                    option.Desc,
                    option.Proto,
                    origin);
                result.Add(d);
            }
        }

        return result;
    }

    public LateJoinGuiMode GetJoinMode()
    {
        return LateJoinMode;
    }

    // public void CloseAllLateJoinGui()
    // {
    //     CloseJoinGui?.Invoke();
    // }

}
