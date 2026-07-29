// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Robocop;

/// <summary>
/// Places round-start RoboCops at a security officer spawn point.
/// </summary>
public sealed class RobocopSpawningSystem : EntitySystem
{
    private static readonly ProtoId<JobPrototype> RobocopJob = "Robocop";
    private static readonly ProtoId<JobPrototype> SecurityOfficerJob = "SecurityOfficer";

    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StationSpawningSystem _spawning = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: [typeof(SpawnPointSystem)]);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null ||
            args.Job != RobocopJob ||
            _ticker.RunLevel == GameRunLevel.InRound)
        {
            return;
        }

        var points = new List<EntityUid>();
        var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var spawnPoint, out var transform))
        {
            if (spawnPoint.SpawnType != SpawnPointType.Job ||
                spawnPoint.Job != SecurityOfficerJob)
            {
                continue;
            }

            if (args.Station != null &&
                _station.GetOwningStation(uid, transform) != args.Station)
            {
                continue;
            }

            points.Add(uid);
        }

        if (points.Count == 0)
            return;

        var coordinates = Transform(_random.Pick(points)).Coordinates;
        args.SpawnResult = _spawning.SpawnPlayerMob(
            coordinates,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
    }
}
