// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Movement.Components;
using Content.Trauma.Shared.Movement;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Movement;

/// <summary>
/// Transfers atmospheric gas into an internal tank.
/// </summary>
public sealed partial class AirSiphonRefillSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private GasTankSystem _gasTank = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AirSiphonRefillComponent, GasTankComponent>();
        while (query.MoveNext(out var uid, out var comp, out var tank))
        {
            if (_timing.CurTime < comp.NextRefill)
                continue;

            comp.NextRefill = _timing.CurTime + TimeSpan.FromSeconds(comp.Interval);

            if (comp.PauseWhileFlying && HasComp<ActiveJetpackComponent>(uid))
                continue;

            if (tank.Air.Pressure >= comp.MaxPressure)
                continue;

            var environment = _atmosphere.GetContainingMixture(uid, excite: true);

            if (environment == null)
                continue;

            var transferMoles = MathF.Min(comp.MolesPerTick, environment.TotalMoles);
            transferMoles = MathF.Min(
                transferMoles,
                AtmosphereSystem.MolesToMaxPressure(environment, tank.Air, comp.MaxPressure));

            if (transferMoles <= 0f)
                continue;

            var scavenged = environment.Remove(transferMoles);
            _atmosphere.Merge(tank.Air, scavenged);
            _gasTank.UpdateUserInterface((uid, tank));
        }
    }
}
