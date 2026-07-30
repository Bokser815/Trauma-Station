// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Components;
using Content.Shared.Mind;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Trauma.Common.Silicons.Borgs;
using Content.Trauma.Shared.Robocop;
using Robust.Shared.Containers;

namespace Content.Trauma.Server.Robocop;

/// <summary>
/// Initializes the chassis modules and handles organic brains.
/// </summary>
public sealed partial class RobocopSystem : SharedRobocopSystem
{
    [Dependency] private SharedBorgSystem _borg = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RobocopChassisComponent, BrainInsertedIntoBorgEvent>(OnBrainInserted);
        SubscribeLocalEvent<RobocopChassisComponent, BrainRemovedFromBorgEvent>(OnBrainRemoved);
    }

    protected override void OnMapInit(Entity<RobocopChassisComponent> ent, ref MapInitEvent args)
    {
        base.OnMapInit(ent, ref args);

        if (!TryComp<BorgChassisComponent>(ent, out var chassis))
            return;

        var chassisEnt = new Entity<BorgChassisComponent>(ent.Owner, chassis);
        var installed = 0;

        foreach (var module in ent.Comp.DefaultModules)
        {
            var moduleEntity = Spawn(module);
            if (!TryComp<BorgModuleComponent>(moduleEntity, out var moduleComp))
            {
                Del(moduleEntity);
                continue;
            }

            _borg.SetBorgModuleDefault((moduleEntity, moduleComp), true);
            _borg.InsertModule(chassisEnt, moduleEntity);

            // InsertModule does not report failure, so don't leave the module lying around if it didn't take.
            // The module was spawned in nullspace, so being in any container means it went into the chassis.
            if (!_container.IsEntityInContainer(moduleEntity))
            {
                Del(moduleEntity);
                continue;
            }

            installed++;
        }

        // Only count the modules that actually made it in, otherwise the chassis is left with unusable slots.
        _borg.SetMaxModules(chassisEnt, ent.Comp.ExtraModuleCount + installed);
    }

    /// <summary>
    /// An organic brain has no <see cref="BorgBrainComponent"/>, so <see cref="SharedBorgSystem"/> leaves the mind
    /// where it is.
    /// </summary>
    private void OnBrainInserted(Entity<RobocopChassisComponent> ent, ref BrainInsertedIntoBorgEvent args)
    {
        if (!IsOrganicBrain(args.Brain))
            return;

        if (_mind.TryGetMind(args.Brain, out var mindId, out var mind))
            _mind.TransferTo(mindId, ent.Owner, mind: mind);
    }

    /// <inheritdoc cref="OnBrainInserted"/>
    private void OnBrainRemoved(Entity<RobocopChassisComponent> ent, ref BrainRemovedFromBorgEvent args)
    {
        if (!IsOrganicBrain(args.Brain) || TerminatingOrDeleted(args.Brain))
            return;

        if (_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            _mind.TransferTo(mindId, args.Brain, mind: mind);
    }

    private bool IsOrganicBrain(EntityUid brain)
    {
        return HasComp<BrainComponent>(brain) && !HasComp<BorgBrainComponent>(brain);
    }
}
