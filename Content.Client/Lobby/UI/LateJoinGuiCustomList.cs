using System.Linq;
using System.Numerics;
using Content.Client.GameTicking.Managers;
using Content.Shared.Roles;
using Content.Shared.Spawning;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Lobby.UI;

public sealed class LateJoinGuiCustomList : DefaultWindow
{
    private readonly Control _base;
    public event Action<(ProtoId<JobPrototype>, NetEntity, int, string)> SelectedOption;

    public LateJoinGuiCustomList(
        IEntityManager entMan,
        ILogManager logManager,
        ClientGameTicker ticker,
        ILobbyManager lobby,
        List<(ProtoId<JobPrototype>, NetEntity?, LocId, LocId)> buttonData,
        LateJoinCustomListOrigin origin)
    {
        var sawmill = logManager.GetSawmill("latejoincustom.panel");
        sawmill.Debug($"Received {buttonData.Count} custom latejoin profiles, sent by {origin}");

        MinSize = SetSize = new Vector2(360, 560);
        Title = Loc.GetString("late-join-custom-list-gui-title");

        _base = new BoxContainer()
        {
            Orientation = LayoutOrientation.Vertical,
            VerticalExpand = true,
        };

        ContentsContainer.AddChild(_base);
        BuildUI(buttonData, ticker);

        SelectedOption += x =>
        {
            var (job, station, number, name) = x;
            sawmill.Info($"Player requesting custom late join spawn: button {number} - '{name}'. {job} on {station}");
            entMan.RaisePredictiveEvent(new LateJoinCustomListEvent( job, station, number, origin ));
            Close();
        };

        lobby.CloseJoinGui += Close;
    }

    private void BuildUI(List<(ProtoId<JobPrototype>, NetEntity?, LocId, LocId)> buttonData, ClientGameTicker ticker)
    {
        var tutorialListScroll = new ScrollContainer() //TODO:ERRANT this doesn't seem to work?
        {
            VerticalExpand = true,
            Visible = false,
        };
        _base.AddChild(tutorialListScroll);

        // Turn the input data into actual buttons
        var counter = 0;
        foreach (var (job, stationInput, nameLoc, descriptionLoc) in buttonData)
        {
            var c = counter; //TODO:ERRANT try again if this is needed
            var name = Loc.GetString(nameLoc);
            var description = Loc.GetString(descriptionLoc);
            var station = stationInput ?? ticker.StationNames.First().Key;

            // We have to validate that the specified station exists
            // if (!ticker.StationNames.ContainsKey(station))
            //     continue;

            var optionLabel = new Label { Margin = new Thickness(5f, 0, 0, 0) };
            var optionButton = new JobButton(optionLabel, job, name, null);
            optionButton.ToolTip = description;

            // We have to validate that the specified station exists
            if (!ticker.StationNames.ContainsKey(station))
            {
                optionButton.Disabled = true;
                //TODO replace tolltip with error?
            }

            var optionSelector = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };

            optionSelector.AddChild(optionLabel);
            optionButton.AddChild(optionSelector);
            optionButton.OnPressed += _ => SelectedOption.Invoke((job, station, c, name));
            _base.AddChild(optionButton);
            counter++;
        }
    }
}
