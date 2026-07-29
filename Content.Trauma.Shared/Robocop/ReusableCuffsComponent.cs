// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Robocop;

/// <summary>
/// Applies a newly fabricated set of cuffs while keeping the tool in hand.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ReusableCuffsComponent : Component
{
    [DataField]
    public EntProtoId HandcuffPrototype = "Handcuffs";

    [DataField]
    public float CuffTime = 3.5f;

    [DataField]
    public float StunBonus = 2f;

    [DataField]
    public SoundSpecifier StartCuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_start.ogg");

    [DataField]
    public SoundSpecifier EndCuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_end.ogg");
}
