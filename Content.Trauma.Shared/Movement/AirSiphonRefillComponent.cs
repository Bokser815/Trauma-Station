// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Movement;

/// <summary>
/// Refills a gas tank from the surrounding atmosphere.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AirSiphonRefillComponent : Component
{
    /// <summary>
    /// Moles transferred per update.
    /// </summary>
    [DataField]
    public float MolesPerTick = 0.4f;

    /// <summary>
    /// Seconds between updates.
    /// </summary>
    [DataField]
    public float Interval = 3f;

    /// <summary>
    /// Maximum tank pressure in kPa.
    /// </summary>
    [DataField]
    public float MaxPressure = 1000f;

    /// <summary>
    /// Whether refilling pauses while the jetpack is active.
    /// </summary>
    [DataField]
    public bool PauseWhileFlying = true;

    /// <summary>
    /// Time of the next update.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextRefill;
}
