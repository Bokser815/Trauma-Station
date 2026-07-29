// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Silicons.Laws;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Trauma.Shared.Silicons.Laws;

namespace Content.Trauma.Server.Silicons.Laws;

/// <summary>
/// Applies a replacement lawset after a silicon is emagged.
/// </summary>
public sealed class EmagLawsetSwapSystem : EntitySystem
{
    [Dependency] private SiliconLawSystem _siliconLaw = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Replace the lawset after the default emag handler runs.
        SubscribeLocalEvent<EmagLawsetSwapComponent, SiliconEmaggedEvent>(OnEmagged, after: [typeof(SiliconLawSystem)]);
    }

    private void OnEmagged(Entity<EmagLawsetSwapComponent> ent, ref SiliconEmaggedEvent args)
    {
        if (!TryComp<SiliconLawProviderComponent>(ent, out var provider))
            return;

        provider.Laws = ent.Comp.Lawset;
        provider.Lawset = _siliconLaw.GetLawset(ent.Comp.Lawset);
        provider.Subverted = true;
    }
}
