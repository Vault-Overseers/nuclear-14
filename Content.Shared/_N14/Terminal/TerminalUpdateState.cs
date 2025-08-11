using System.Collections.Generic;
using Robust.Shared.Serialization;
using Content.Shared.CartridgeLoader;

namespace Content.Shared._N14.Terminal;

/// <summary>
/// UI state for terminals showing available programs and the active program UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class TerminalUpdateState : CartridgeLoaderUiState
{
    public TerminalUpdateState(List<NetEntity> programs, NetEntity? activeUi)
        : base(programs, activeUi)
    {
    }
}
