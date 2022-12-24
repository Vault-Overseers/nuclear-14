using Content.Server.Body.Components;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RemoveMechanismCommand : IConsoleCommand
    {
        public string Command => "rmmechanism";
        public string Description => "Removes a given entity from it's containing bodypart, if any.";
        public string Help => "Usage: rmmechanism <uid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!EntityUid.TryParse(args[0], out var entityUid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetComponent<TransformComponent>(entityUid, out var transform)) return;

            var parent = transform.ParentUid;

            if (entityManager.TryGetComponent<BodyPartComponent>(parent, out var body) &&
                entityManager.TryGetComponent<MechanismComponent>(entityUid, out var part))
            {
                body.RemoveMechanism(part);
            }
            else
            {
                shell.WriteError("Was not a mechanism, or did not have a parent.");
            }
        }
    }
}
