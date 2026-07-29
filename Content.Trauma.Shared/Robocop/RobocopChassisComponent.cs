// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;

namespace Content.Trauma.Shared.Robocop;

/// <summary>
/// Adds an organ slot and fixed modules to a borg chassis.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRobocopSystem))]
public sealed partial class RobocopChassisComponent : Component
{
    /// <summary>
    /// The lung slot ID.
    /// </summary>
    [DataField]
    public string LungSlotId = "robocop_lungs";

    /// <summary>
    /// The lung slot.
    /// </summary>
    [DataField(required: true)]
    public ItemSlot LungSlot = new();

    /// <summary>
    /// Modules installed when the chassis is initialized.
    /// </summary>
    [DataField]
    public List<EntProtoId> DefaultModules = new();

    /// <summary>
    /// Additional module capacity beyond <see cref="DefaultModules"/>.
    /// </summary>
    [DataField]
    public int ExtraModuleCount;

    /// <summary>
    /// Minimum time between warnings when movement is blocked by missing lungs.
    /// </summary>
    [DataField]
    public TimeSpan LungWarningCooldown = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The next time a missing-lung movement warning can be shown.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextLungWarning;
}
