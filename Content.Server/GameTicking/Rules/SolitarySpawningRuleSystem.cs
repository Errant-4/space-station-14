using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Prototypes;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Roles;
using Content.Shared.Spawning;
using Content.Shared.Station.Components;
using Linguini.Shared.Util;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

// TODO Integration test

/// <summary>
/// This system overrides the normal spawn process, and puts each player on their own personal map.
/// </summary>
/// <remarks>
/// Currently, this always targets every player.
/// The main station will still spawn, but no one will ever be on it. As such, when this game rule is in use,
/// the server should be forced to use the 'Empty' map, to avoid spawning a bunch of unnecessary entities and active mobs
/// </remarks>>
public sealed class SolitarySpawningSystem : GameRuleSystem<SolitarySpawningRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedGameTicker _ticker2 = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly Dictionary<ICommonSession, EntityUid> _stations = [];

    private Dictionary<ICommonSession, int> _choices = new();
    private ProtoId<JobPrototype> _job = "Passenger";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<LobbyLateJoinButtonPressedEvent>(OnLateJoinButton);
        SubscribeNetworkEvent<LateJoinCustomListEvent>(OnCustomList);
    }

    private void OnLateJoinButton(LobbyLateJoinButtonPressedEvent message, EntitySessionEventArgs args)
    {
        var a = this;
        var b = a.ToString();

        ProtoId<JobPrototype> job = "Passenger"; // This will be overwritten by the actual Spawn Profile later

        //TODO check active rules and make the list
        var station = new NetEntity();

        var buttonData = new List<(ProtoId<JobPrototype>, NetEntity?, LocId, LocId)>();
        buttonData.Add((_job, null, "Tutorial", "This is the first tutorial"));
        buttonData.Add((_job, null, "Death", "This will kill you"));
        buttonData.Add((_job, new NetEntity(), "Out of Order", "Even in the future, nothing works."));

        var ev = new SolitarySpawningGuiDataEvent(buttonData, LateJoinCustomListOrigin.SolitarySpawningSystem);
        RaiseNetworkEvent(ev); //TODO:ERRANT test without channel

        message.Handled = true;
    }

    private void OnCustomList(LateJoinCustomListEvent message, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        var station = _entity.GetEntity(message.Station);

        _choices.Remove(session);
        _choices.Add(session, message.ButtonId);

        //TODO checks?
        _gameTicker.MakeJoinGame(session, station, message.Job, silent:true);

    }

    private bool ActiveRules()
    {
        var rules = EntityQueryEnumerator<SolitarySpawningRuleComponent, GameRuleComponent>();

        while (rules.MoveNext(out var uid, out var comp, out var rule))
        {
        }

        return false;
    }

    /// <summary>
    /// A player is trying to enter the round, or is respawning
    /// </summary>
    private void OnBeforeSpawn(PlayerBeforeSpawnEvent args)
    {
        var session = args.Player;
        var active = false;

        // Check if any Solitary Spawning rules are running
        var rules = EntityQueryEnumerator<SolitarySpawningRuleComponent,GameRuleComponent>();
        while (rules.MoveNext(out var uid, out var comp, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            // TODO check blacklists/whitelists from the gamerule

            // Only need to report failure if the player was covered under any active rules
            active = true;

            var count = comp.Prototypes.Count;
            if (count <= 0)
            {
                Log.Warning($"No solitary spawning prototypes were included in '{ToPrettyString(uid)}'.");
                continue;
            }

            //TODO query SolitarySpawningManager which option the player picked when joining
            int? playerChoice = null;
            if (_choices.TryGetValue(args.Player, out var found))
                playerChoice = found;
            _choices.Remove(args.Player);

            if (playerChoice is null || !playerChoice.Value.InRange(0, count - 1))
            {
                Log.Warning($"Received invalid player choice for '{session}'. Received option: '{playerChoice}'. " +
                            $"The gamerule '{ToPrettyString(uid)}' has {count} prototypes. " +
                            $"Defaulting to first option: '{comp.Prototypes[0].Id}'");

                playerChoice = 0;
            }

            var chosenProtoId = comp.Prototypes[playerChoice.Value];

            if (!_proto.TryIndex(chosenProtoId, out var proto))
            {
                Log.Warning($"Solitary spawning failed for {session} - chosen prototype '{chosenProtoId}' does not exist");
                continue;
            }
            Log.Debug($"Solitary spawning prototype '{chosenProtoId}' selected for {session}");

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
        [NotNullWhen(true)]out EntityUid? stationTarget)
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

        //store the newly created station entity for this session so we can put the player back on respawn
        _stations.Add(session, stationTarget.Value);
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

        station = stored;
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
    }
}
