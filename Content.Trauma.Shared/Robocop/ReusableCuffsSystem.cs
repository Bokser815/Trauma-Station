// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Administration.Components;
using Content.Shared.Coordinates;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Robocop;

public sealed class ReusableCuffsSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedCuffableSystem _cuffs = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReusableCuffsComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ReusableCuffsComponent, ReusableCuffsDoAfterEvent>(OnCuff);
    }

    private void OnAfterInteract(Entity<ReusableCuffsComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { Valid: true } target)
            return;

        if (!args.CanReach || !CanCuff(args.User, target))
            return;

        var cuffTime = ent.Comp.CuffTime;
        if (HasComp<StunnedComponent>(target))
            cuffTime = MathF.Max(0.1f, cuffTime - ent.Comp.StunBonus);

        if (HasComp<DisarmProneComponent>(target))
            cuffTime = 0f;

        var doAfter = new DoAfterArgs(
            EntityManager,
            args.User,
            cuffTime,
            new ReusableCuffsDoAfterEvent(),
            ent.Owner,
            target,
            ent.Owner)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
            NeedHand = true,
            DistanceThreshold = 1f,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        args.Handled = true;
        _audio.PlayPredicted(ent.Comp.StartCuffSound, ent.Owner, args.User);
    }

    private void OnCuff(Entity<ReusableCuffsComponent> ent, ref ReusableCuffsDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Cancelled || args.Target is not { Valid: true } target)
            return;

        var user = args.Args.User;
        if (!CanCuff(user, target))
            return;

        var handcuffs = PredictedSpawnAtPosition(ent.Comp.HandcuffPrototype, ent.Owner.ToCoordinates());
        if (!_cuffs.TryAddNewCuffs(target, user, handcuffs))
        {
            PredictedQueueDel(handcuffs);
            return;
        }

        _audio.PlayPredicted(ent.Comp.EndCuffSound, ent.Owner, user);

        var popup = user == target
            ? "handcuff-component-cuff-self-observer-success-message"
            : "handcuff-component-cuff-observer-success-message";
        _popup.PopupEntity(
            Loc.GetString(
                popup,
                ("user", Identity.Name(user, EntityManager)),
                ("target", Identity.Entity(target, EntityManager))),
            target,
            Filter.Pvs(target, entityManager: EntityManager)
                .RemoveWhere(session => session.AttachedEntity == target || session.AttachedEntity == user),
            true);

        if (user == target)
        {
            _popup.PopupEntity(Loc.GetString("handcuff-component-cuff-self-success-message"), user, user);
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} has cuffed themselves");
            return;
        }

        _popup.PopupEntity(
            Loc.GetString(
                "handcuff-component-cuff-other-success-message",
                ("otherName", Identity.Name(target, EntityManager, user))),
            user,
            user);
        _popup.PopupEntity(
            Loc.GetString(
                "handcuff-component-cuff-by-other-success-message",
                ("otherName", Identity.Name(user, EntityManager, target))),
            target,
            target);
        _adminLog.Add(
            LogType.Action,
            LogImpact.High,
            $"{ToPrettyString(user):player} has cuffed {ToPrettyString(target):player}");
    }

    private bool CanCuff(EntityUid user, EntityUid target)
    {
        if (!_interaction.InRangeUnobstructed(user, target))
            return false;

        if (!TryComp<CuffableComponent>(target, out var cuffable) ||
            !TryComp<HandsComponent>(target, out var hands))
        {
            return false;
        }

        return cuffable.CuffedHandCount < hands.Count;
    }
}

[Serializable, NetSerializable]
public sealed partial class ReusableCuffsDoAfterEvent : SimpleDoAfterEvent;
