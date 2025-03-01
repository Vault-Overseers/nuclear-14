/*
Copyright (C) 2025 Stalker14
license:
  This source code is the exclusive of TornadoTech (Maltsev Daniil), JerryImMouse, LordVladimer (Valdis Fedorov)
  and is protected by copyright law.
  Any unauthorized use or reproduction of this source code
  is strictly prohibited and may result in legal action.
  For inquiries or licensing requests,
  please contact TornadoTech (Maltsev Daniil), JerryImMouse, LordVladimer (Valdis Fedorov)
  at Discord (https://discord.com/invite/pu6DEPGjsN).
*/
using Robust.Shared.Serialization;

namespace Content.Shared.Crafting.Events;

[Serializable, NetSerializable]
public sealed class DisassembleStartedEvent : EntityEventArgs
{
    public readonly NetEntity StorageEnt;

    public DisassembleStartedEvent(NetEntity storageEnt)
    {
        StorageEnt = storageEnt;
    }
}
