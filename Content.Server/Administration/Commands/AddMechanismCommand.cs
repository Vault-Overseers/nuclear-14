using Content.Server.Body.Components;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddMechanismCommand : IConsoleCommand
    {
        public string Command => "addmechanism";
        public string Description => "Adds a given entity to a containing body.";
        public string Help => "Usage: addmechanism <entity uid> <bodypart uid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!EntityUid.TryParse(args[0], out var entityUid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            if (!EntityUid.TryParse(args[1], out var storageUid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (entityManager.TryGetComponent<BodyPartComponent>(storageUid, out var storage)
                && entityManager.TryGetComponent<MechanismComponent>(entityUid, out var bodyPart))
            {
                if (storage.TryAddMechanism(bodyPart))
                {
                    shell.WriteLine($@"Added {entityUid} to {storageUid}.");
                }
                else
                {
                    shell.WriteError($@"Could not add {entityUid} to {storageUid}.");
                }
            }
            else
            {
                shell.WriteError("Could not insert.");
            }
        }
    }
}
