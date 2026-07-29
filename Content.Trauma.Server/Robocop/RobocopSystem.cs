// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Wires;
using Content.Trauma.Shared.Robocop;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Robocop;

/// <summary>
/// Initializes the chassis modules and occupant restrictions.
/// </summary>
public sealed class RobocopSystem : SharedRobocopSystem
{
    private const string BrainContainerId = "borg_brain";

    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedBorgSystem _borg = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RobocopChassisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RobocopChassisComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<RobocopChassisComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<RobocopChassisComponent, AfterInteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RobocopChassisComponent, EntInsertedIntoContainerMessage>(OnBrainInserted);
        SubscribeLocalEvent<RobocopChassisComponent, EntRemovedFromContainerMessage>(OnBrainRemoved);
    }

    private void OnMapInit(Entity<RobocopChassisComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.DefaultModules.Count == 0)
            return;

        if (!TryComp<BorgChassisComponent>(ent, out var chassis))
            return;

        var chassisEnt = new Entity<BorgChassisComponent>(ent.Owner, chassis);

        _borg.SetMaxModules(chassisEnt, ent.Comp.ExtraModuleCount + ent.Comp.DefaultModules.Count);

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
        }
    }

    private void OnMindAdded(Entity<RobocopChassisComponent> ent, ref MindAddedMessage args)
    {
        SetTrapped(args.Mind, ent.Comp.TrapOccupant);
    }

    private void OnMindRemoved(Entity<RobocopChassisComponent> ent, ref MindRemovedMessage args)
    {
        SetTrapped(args.Mind, false);
    }

    private void OnInteractUsing(Entity<RobocopChassisComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled ||
            !args.CanReach ||
            args.User == ent.Owner ||
            !HasComp<BrainComponent>(args.Used))
        {
            return;
        }

        if (!_container.TryGetContainer(ent.Owner, BrainContainerId, out var brainContainer) ||
            brainContainer.Count != 0)
        {
            return;
        }

        if (TryComp<WiresPanelComponent>(ent, out var panel) && !panel.Open)
        {
            _popup.PopupEntity(Loc.GetString("borg-panel-not-open"), ent, args.User);
            return;
        }

        if (TryComp<ActorComponent>(args.Used, out var actor) &&
            !_borg.CanPlayerBeBorged(actor.PlayerSession))
        {
            _popup.PopupEntity(Loc.GetString("borg-player-not-allowed"), args.Used, args.User);
            return;
        }

        if (!_container.Insert(args.Used, brainContainer))
            return;

        _adminLog.Add(LogType.Action,
            LogImpact.Medium,
            $"{args.User} installed brain {args.Used} into RoboCop {ent.Owner}");
        args.Handled = true;
    }

    private void OnBrainInserted(Entity<RobocopChassisComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != BrainContainerId ||
            !HasComp<BrainComponent>(args.Entity) ||
            HasComp<BorgBrainComponent>(args.Entity))
        {
            return;
        }

        if (_mind.TryGetMind(args.Entity, out var mindId, out var mind))
            _mind.TransferTo(mindId, ent.Owner, mind: mind);
    }

    private void OnBrainRemoved(Entity<RobocopChassisComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != BrainContainerId ||
            !HasComp<BrainComponent>(args.Entity) ||
            HasComp<BorgBrainComponent>(args.Entity))
        {
            return;
        }

        if (_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            _mind.TransferTo(mindId, args.Entity, mind: mind);
    }

    private void SetTrapped(Entity<MindComponent> mind, bool trapped)
    {
        mind.Comp.PreventGhosting = trapped;
        mind.Comp.PreventSuicide = trapped;
    }
}
