// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Silicons.Laws;

namespace Content.Trauma.Shared.Silicons.Laws;

/// <summary>
/// Replaces a silicon's lawset when emagged.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EmagLawsetSwapComponent : Component
{
    /// <summary>
    /// The replacement lawset.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SiliconLawsetPrototype> Lawset;
}
