using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Prototypes;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Lobby;
using Content.Shared.Roles;
using Content.Shared.Spawning;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

// TODO:ERRANT Integration test

/// <summary>
/// This system overrides the normal spawn process, and puts each player on their own personal map.
/// </summary>
/// <remarks>
/// Currently, this always targets every player.
/// The main station will still spawn, but no one will ever be on it. As such, when this game rule is in use,
/// the server should be set to some lightweight map, like Empty
/// </remarks>
public sealed class SolitarySpawningSystem : GameRuleSystem<SolitarySpawningRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    // A list of the station entities generated for each player (and the map they are on).
    // Used for respawning players on their own station, and for deleting unused maps.
    private readonly Dictionary<ICommonSession, (EntityUid, MapId)> _stations = [];

    // When a player picks a spawn option on the Join GUI, it's stored here until they go through the player spawn process
    private readonly Dictionary<ICommonSession, ProtoId<SolitarySpawningPrototype>> _choices = new();

    // The spawn prototype will override the job, but we need to set an initial value to begin the spawn process.
    // Normally this is provided by the player's pick on the late join GUI, but we don't have that with the custom GUI, so we use this dummy
    private readonly ProtoId<JobPrototype> _job = "Passenger";

    // The list of currently running Solitary Spawning Rules is stored here, so it does not need to be queried redundantly
    private List<SolitarySpawningRuleComponent> _rules = [];
    private bool _rulesActive;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolitarySpawningRuleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SolitarySpawningRuleComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<LobbyLateJoinButtonPressedEvent>(OnLateJoinButton);
        SubscribeNetworkEvent<LateJoinCustomListEvent>(OnCustomList);
    }

    private void OnStartup(Entity<SolitarySpawningRuleComponent> ent, ref ComponentStartup args)
    {
        _rules.Add(ent.Comp);
        RuleActiveUpdate(true);

        // ProtoId<JobPrototype> job = "Passenger"; //TODO:ERRANT This will be overwritten by the actual Spawn Profile later

        //TODO check active rules and make the list
        var station = new NetEntity();

        //TODO:ERRANT LATER make button data from prototype

        var buttonData = new List<(ProtoId<JobPrototype>, NetEntity?, LocId, LocId, string)>(); //TODO:ERRANT send ProtoId<SolitarySpawningPrototype> instead of string
        buttonData.Add((_job, null, "Tutorial", "This is the first tutorial", "TutorialTest"));
        buttonData.Add((_job, null, "Death", "This will kill you", "Death"));
        buttonData.Add((_job, station, "Out of Order", "Even in the future, nothing works.", "ProtoDud"));

        var ev = new SolitarySpawningGuiDataEvent(buttonData, LateJoinCustomListOrigin.SolitarySpawningSystem);
        RaiseNetworkEvent(ev); //TODO:ERRANT test without channel
    }

    private void OnShutdown(Entity<SolitarySpawningRuleComponent> ent, ref ComponentShutdown args)
    {
        UpdateRules();
    }

    /// <summary>
    /// Updates _rules to show whether any solitary spawning rules are currently active.
    /// </summary>
    private void UpdateRules()
    {
        var list = new List<SolitarySpawningRuleComponent>();
        var active = false;
        var query = QueryActiveRules();

        while (query.MoveNext(out var uid, out var comp, out var rule))
        {
            list.Add(comp);
            active = true;
        }

        RuleActiveUpdate(active);

        _rules = list;
    }

    // Let the client Lobbies know when they need to switch to/from custom spawn Gui TODO:ERRANT write this better
    private void RuleActiveUpdate(bool newStatus)
    {
        if (newStatus == _rulesActive)
            return;

        //TODO This should be updated with the feature to put only specific clients into Custom mode, specified by the prototype

        var newval = newStatus ? LateJoinGuiMode.CustomList : LateJoinGuiMode.Default;

        var ev = new ChangeLateJoinGuiModeEvent(newval);
        RaiseNetworkEvent(ev);
        _rulesActive = newStatus;
    }

    /// <summary>
    /// A player pressed the Late Join button
    /// </summary>
    private void OnLateJoinButton(LobbyLateJoinButtonPressedEvent message, EntitySessionEventArgs args)
    {
        UpdateRules(); //TODO:ERRANT This should not be here, but the normal call in line 100 fails to detect the new rule

        if (!_rulesActive)
            return;

        var buttonData = new List<(ProtoId<JobPrototype>, NetEntity?, LocId, LocId, string)>();

        Log.Debug($"Creating button data for {args.SenderSession}. Rules found: {_rules.Count}");
        foreach (var rule in _rules)
        {
            foreach (var protoId in rule.Prototypes)
            {
                if (_proto.TryIndex(protoId, out var proto))
                    buttonData.Add((_job, null, proto.Name, proto.Description, protoId));
            }
        }

        buttonData.Add((_job, new NetEntity(), "Out of Order", "Even in the future, nothing works.", "")); //TODO:ERRANT remove

        Log.Debug($"Created {buttonData.Count} button entries");

        var ev = new SolitarySpawningGuiDataEvent(buttonData, LateJoinCustomListOrigin.SolitarySpawningSystem);
        RaiseNetworkEvent(ev, args.SenderSession);
    }

    /// <summary>
    /// Server received a custom list GUI choice from the client
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    private void OnCustomList(LateJoinCustomListEvent message, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        var station = _entity.GetEntity(message.Station);

        _choices.Remove(session);
        _choices.Add(session, message.ButtonId);

        //TODO:ERRANT check origin ?
        _gameTicker.MakeJoinGame(session, station, message.Job, silent:true);

    }

    // private bool ActiveRules(out ) //TODO:ERRANT probably no longer needed
    // {
    //     var rules = QueryActiveRules();
    //
    //     while (rules.MoveNext(out var uid, out var comp, out var rule))
    //     {
    //     }
    //
    //     return false;
    // }

    /// <summary>
    /// A player is trying to enter the round, or is respawning
    /// </summary>
    private void OnBeforeSpawn(PlayerBeforeSpawnEvent args)
    {
        var session = args.Player;
        var active = false;

        // Check if any Solitary Spawning rules are running
        foreach (var comp in _rules)
        {
            // TODO check blacklists/whitelists from the gamerule

            // Only need to report failure if the player was covered under any active rules
            active = true;

            if (comp.Prototypes.Count == 0)
            {
                Log.Warning("No prototypes were included in SolitarySpawningRuleComponent");

                continue;
            }

            ProtoId<SolitarySpawningPrototype>? playerChoice = null;

            //TODO query SolitarySpawningManager which option the player picked when joining

            if (_choices.TryGetValue(args.Player, out var found))
                playerChoice = found;
            _choices.Remove(args.Player);

            if (playerChoice is null || !comp.Prototypes.Contains(playerChoice.Value))
            {
                Log.Warning($"Received invalid player choice from '{session}'. Player chose '{playerChoice}'. " +
                            $"Defaulting to first option: '{comp.Prototypes.First().Id}'");

                playerChoice = comp.Prototypes.First();
            }

            if (!_proto.TryIndex(playerChoice, out var proto))
            {
                Log.Warning($"Solitary spawning failed for {session} - prototype '{playerChoice}' does not exist");
                continue;
            }
            Log.Debug($"Solitary spawning prototype '{playerChoice}' selected for {session}");

            var job = proto.Job;

            if (RequestExistingStation(session, out var stationExist))
            {
                Log.Debug($"Existing solitary station found for {session}. Not creating a new map.");
                SpawnPlayer(args, job, stationExist.Value, null);
                args.Handled = true;
                break;
            }

            if (!CreateSolitaryStation(args, proto, session, out var stationTarget))
                continue;

            SpawnPlayer(args, job, stationTarget.Value, proto.WelcomeLoc);
            args.Handled = true;
            break;
        }

        // If any SolitarySpawningRules are active, it is likely a notable malfunction if a player spawns normally
        if (active && args.Handled == false)
            Log.Warning($"Solitary spawning failed for {session.Name}, spawning on the normal map.");
    }

    /// <summary>
    /// Create a personal map for one player
    /// </summary>
    /// <param name="args">The spawn event for the player trying to spawn</param>
    /// <param name="prototype">The prototype that specifies the map and job for the spawn</param>
    /// <param name="session">The player session</param>
    /// <param name="stationTarget">The station entity (nullspace) that is being created for this player</param>
    private bool CreateSolitaryStation(
        PlayerBeforeSpawnEvent args,
        SolitarySpawningPrototype prototype,
        ICommonSession session,
        [NotNullWhen(true)] out EntityUid? stationTarget)
    {
        stationTarget = null;
        var proto = prototype.Map;

        if (!_proto.TryIndex(proto, out var map))
        {
            Log.Error($"Solitary spawning failed for {session} - Invalid map prototype: {proto}");
            return false;
        }

        // Create the new map and station, and assign them identifiable names
        var stationName = Loc.GetString("solitary-station-name", ("character", args.Profile.Name));
        var mapName = Loc.GetString("solitary-map-name", ("character", args.Profile.Name));
        var query = GameTicker.LoadGameMap(map, out var mapId, stationName: stationName);
        var newMap = query.First();
        _meta.SetEntityName(Transform(newMap).ParentUid, mapName);

        _map.InitializeMap(mapId);

        if (!TryComp<StationMemberComponent>(newMap, out var member))
        {
            Log.Error($"Solitary spawning failed for {session} - Target station not found");
            return false;
        }

        stationTarget = member.Station;

        //store the newly created station entity and map for this session, for respawn and cleanup purposes
        _stations.Add(session, (stationTarget.Value, mapId));
        return true;
    }

    /// <summary>
    /// Spawn the player and their gear
    /// </summary>
    /// <param name="args">The spawn event</param>
    /// <param name="jobId">The prototype ID of the job that will be assigned to the player</param>
    /// <param name="station">The station entity (nullspace)</param>
    /// <param name="message">A message that will be announced to the player upon spawning on the map for the first time</param>
    private void SpawnPlayer(PlayerBeforeSpawnEvent args, ProtoId<JobPrototype> jobId, EntityUid station, LocId? message)
    {
        var session = args.Player;
        var humanoid = args.Profile;

        GameTicker.DoSpawn(session, humanoid, station, jobId, true, out var mob, out _, out var jobName);

        // Latejoin is not a relevant concept for solitary spawns - the station did not even exist beforehand
        // Also, round flow does not exist in the regular sense on a tutorial server
        // So all spawns are recorded as just "Joined"
        _adminLogger.Add(LogType.RoundStartJoin,
            LogImpact.Medium,
            $"Player {session.Name} has spawned on a solitary map. Joined as {humanoid.Name:characterName} on station {Name(station):stationName} with {ToPrettyString(mob):entity} as a {jobName:jobName}.");

        if (message is not null)
            _chatManager.DispatchServerMessage(session, Loc.GetString(message));
    }

    /// <summary>
    /// Checks if a player already has a station allocated to them.
    /// </summary>
    /// <param name="session">The player session</param>
    /// <param name="station">The player's existing station</param>
    /// <returns></returns>
    private bool RequestExistingStation(ICommonSession session, [NotNullWhen(true)] out EntityUid? station)
    {
        station = null;

        if (!_stations.TryGetValue(session, out var stored))
            return false;

        station = stored.Item1;
        return true;
    }

    /// Clear the saved station list, since the maps are being deleted
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _stations.Clear();
    }

    private void MapCleanup()
    {
        // TODO map cleanup x minutes after the player left the server, or if they go back to the lobby

        // TODO map should also clean up when the player is sent back to the lobby, otherwise they can't select a different tutorial
    }
}
