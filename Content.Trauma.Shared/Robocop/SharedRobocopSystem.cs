// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ActionBlocker;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Standing;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Robocop;

/// <summary>
/// Handles the organ requirement for a <see cref="RobocopChassisComponent"/>.
/// </summary>
public abstract partial class SharedRobocopSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedBorgSystem _borg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RobocopChassisComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RobocopChassisComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<RobocopChassisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RobocopChassisComponent, BorgCanActivateEvent>(OnCanActivate);
        SubscribeLocalEvent<RobocopChassisComponent, EntInsertedIntoContainerMessage>(OnLungsInserted);
        SubscribeLocalEvent<RobocopChassisComponent, EntRemovedFromContainerMessage>(OnLungsRemoved);
        SubscribeLocalEvent<RobocopChassisComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<RobocopChassisComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<RobocopChassisComponent, UpdateCanMoveEvent>(OnCanMove);
    }

    private void OnInit(Entity<RobocopChassisComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.LungSlotId, ent.Comp.LungSlot);
    }

    private void OnRemove(Entity<RobocopChassisComponent> ent, ref ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.LungSlot);
    }

    protected virtual void OnMapInit(Entity<RobocopChassisComponent> ent, ref MapInitEvent args)
    {
        UpdateLungState(ent, false);
    }

    private void OnCanActivate(Entity<RobocopChassisComponent> ent, ref BorgCanActivateEvent args)
    {
        if (!HasLungs(ent))
            args.Cancelled = true;
    }

    private void OnLungsInserted(Entity<RobocopChassisComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked with the same game state.

        if (args.Container.ID != ent.Comp.LungSlotId)
            return;

        UpdateLungState(ent, false);
    }

    private void OnLungsRemoved(Entity<RobocopChassisComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return; // The changes are already networked with the same game state.

        if (args.Container.ID != ent.Comp.LungSlotId)
            return;

        UpdateLungState(ent, true);
        _popup.PopupEntity(Loc.GetString("robocop-lungs-removed"), ent);
    }

    private void OnMoveInput(Entity<RobocopChassisComponent> ent, ref MoveInputEvent args)
    {
        if (HasLungs(ent) || !args.HasDirectionalMovement || _timing.CurTime < ent.Comp.NextLungWarning)
            return;

        ent.Comp.NextLungWarning = _timing.CurTime + ent.Comp.LungWarningCooldown;
        _popup.PopupEntity(
            Loc.GetString("robocop-lungs-missing-movement"),
            ent,
            ent,
            PopupType.SmallCaution);
    }

    private void OnStandAttempt(Entity<RobocopChassisComponent> ent, ref StandAttemptEvent args)
    {
        if (!HasLungs(ent))
            args.Cancel();
    }

    private void OnCanMove(Entity<RobocopChassisComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (!HasLungs(ent))
            args.Cancel();
    }

    private void UpdateLungState(Entity<RobocopChassisComponent> ent, bool playDownSound)
    {
        var hasLungs = HasLungs(ent);

        if (TryComp<BorgChassisComponent>(ent, out var chassis))
        {
            if (hasLungs)
                _borg.TryActivate((ent.Owner, chassis));
            else
                _borg.SetActive((ent.Owner, chassis), false);
        }

        if (hasLungs)
        {
            if (_mobState.IsAlive(ent))
                _standing.Stand(ent.Owner);
        }
        else
        {
            _standing.Down(
                ent.Owner,
                playSound: playDownSound,
                dropHeldItems: false,
                force: true);
        }

        _actionBlocker.UpdateCanMove(ent.Owner);
    }

    /// <summary>
    /// Returns whether the lung slot is occupied.
    /// </summary>
    public bool HasLungs(Entity<RobocopChassisComponent> ent)
    {
        return ent.Comp.LungSlot.HasItem;
    }
}
