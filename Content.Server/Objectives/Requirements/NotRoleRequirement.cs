using Content.Shared.Roles;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Objectives.Requirements
{

    [DataDefinition]
    public sealed class NotRoleRequirement : IObjectiveRequirement
    {
        [DataField("roleId", customTypeSerializer:typeof(PrototypeIdSerializer<JobPrototype>))]
        private string roleId = default!;

        /// <summary>
        /// This requirement is met if the traitor is NOT the roleId, and fails if they are.
        /// </summary>
        public bool CanBeAssigned(Mind.Mind mind)
        {
            if (mind.CurrentJob == null) // no job no problems
                return true;

            return (mind.CurrentJob.Prototype.ID != roleId);
        }
    }

    [DataDefinition]
    public sealed class RoleRequirement : IObjectiveRequirement
    {
        [DataField("roleId", customTypeSerializer:typeof(PrototypeIdSerializer<JobPrototype>))]
        private string roleId = default!;

        /// <summary>
        /// This requirement is met if the mind has the roleId.
        /// </summary>
        public bool CanBeAssigned(Mind.Mind mind)
        {
            if (mind.CurrentJob == null) // no job no problems
                return false;

            return (mind.CurrentJob.Prototype.ID == roleId);
        }
    }
}
