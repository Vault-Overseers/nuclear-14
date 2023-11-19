﻿using Content.Server.Administration;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Ghost.Roles
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class MakeGhostRoleCommand : IConsoleCommand
    {
        public string Command => "makeghostrole";
        public string Description => "Turns an entity into a ghost role.";
        public string Help => $"Usage: {Command} <entity uid> <name> <description> [<rules>]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3 || args.Length > 4)
            {
                shell.WriteLine($"Invalid amount of arguments.\n{Help}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!EntityUid.TryParse(args[0], out var uid))
            {
                shell.WriteLine($"{args[0]} is not a valid entity uid.");
                return;
            }

            if (!entityManager.TryGetComponent(uid, out MetaDataComponent? metaData))
            {
                shell.WriteLine($"No entity found with uid {uid}");
                return;
            }

            if (entityManager.TryGetComponent(uid, out MindContainerComponent? mind) &&
                mind.HasMind)
            {
                shell.WriteLine($"Entity {metaData.EntityName} with id {uid} already has a mind.");
                return;
            }

            var name = args[1];
            var description = args[2];
            var rules = args.Length >= 4 ? args[3] : Loc.GetString("ghost-role-component-default-rules");

            if (entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
            {
                shell.WriteLine($"Entity {metaData.EntityName} with id {uid} already has a {nameof(GhostRoleComponent)}");
                return;
            }

            if (entityManager.TryGetComponent(uid, out GhostTakeoverAvailableComponent? takeOver))
            {
                shell.WriteLine($"Entity {metaData.EntityName} with id {uid} already has a {nameof(GhostTakeoverAvailableComponent)}");
                return;
            }

            ghostRole = entityManager.AddComponent<GhostRoleComponent>(uid);
            entityManager.AddComponent<GhostTakeoverAvailableComponent>(uid);
            ghostRole.RoleName = name;
            ghostRole.RoleDescription = description;
            ghostRole.RoleRules = rules;

            shell.WriteLine($"Made entity {metaData.EntityName} a ghost role.");
        }
    }
}
