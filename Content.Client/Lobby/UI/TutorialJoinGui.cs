using System.Linq;
using System.Numerics;
using Content.Client.GameTicking.Managers;
using Content.Shared.Tutorial;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Player;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Lobby.UI;

public sealed class TutorialJoinGui : DefaultWindow
{
    private readonly Control _base;
    public event Action<(NetEntity, int, string)> SelectedOption;
    private readonly ClientGameTicker _gameTicker;

    // This will be overwritten by SolitarySpawningRuleSystem,
    // but before we get there, we need to tell the server something.
    private const string Job = "Passenger";

    public TutorialJoinGui(
        IClientConsoleHost consoleHost,
        IEntityManager entMan,
        ILogManager logManager,
        ICommonSession session,
        ClientGameTicker ticker)
    {
        var sawmill = logManager.GetSawmill("tutorialjoin.panel");
        _gameTicker = ticker;

        MinSize = SetSize = new Vector2(360, 560);
        Title = Loc.GetString("tutorial-join-gui-title");

        _base = new BoxContainer()
        {
            Orientation = LayoutOrientation.Vertical,
            VerticalExpand = true,
        };

        ContentsContainer.AddChild(_base);

        // TODO Rebuild when the Manager updates spawn prototypes

        RebuildUI();

        SelectedOption += x =>
        {
            var (station, number, name) = x;
            sawmill.Info($"Player requesting server to Solitary Spawn: button {number} - '{name}'");
            // consoleHost.ExecuteCommand($"joingame {Job} {station}");
            entMan.RaisePredictiveEvent(new TutorialJoinEvent( Job, station, number )); //TODO:ERRANT
            Close();
        };
    }

    private void RebuildUI()
    {
        _base.RemoveAllChildren();
        var tutorials = new List<(LocId, LocId)>();

        // TODO ask the Manager to provide a list of tutorial options
        tutorials = new List<(LocId, LocId)>();
        tutorials.Add(("Tutorial 1", "This is the first tutorial"));
        tutorials.Add(("Tutorial 2", "This is the second tutorial"));
        tutorials.Add(("Tutorial 3", "This is the third tutorial"));
        // TODO

        // We specifically only want one set of "options", so no need to get every station.
        // There will be as many stations as players, but they are not important. We just need to
        // spawn a new player anywhere, and then let SolitarySpawningRuleSystem to handle the rest
        var anyStation = _gameTicker.StationNames.First().Key;

        var tutorialListScroll = new ScrollContainer()
        {
            VerticalExpand = true,
            Visible = false,
        };
        _base.AddChild(tutorialListScroll);

        var counter = 0;
        foreach (var option in tutorials)
        {
            var c = counter;
            var optionLabel = new Label { Margin = new Thickness(5f, 0, 0, 0) };
            var optionButton = new JobButton(optionLabel, Job, option.Item1, null);
            optionButton.ToolTip = option.Item2;

            var optionSelector = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };

            optionSelector.AddChild(optionLabel);
            optionButton.AddChild(optionSelector);
            optionButton.OnPressed += _ => SelectedOption.Invoke((anyStation, c, option.Item1));
            _base.AddChild(optionButton);
            counter++;
        }
    }
}
