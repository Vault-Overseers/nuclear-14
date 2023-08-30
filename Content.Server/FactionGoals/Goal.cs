using Content.Server.NPC.Components;

namespace Content.Server.FactionGoals
{
    public sealed class Goal : IEquatable<Goal>
    {
        [ViewVariables]
        public readonly Mind.Mind Mind;
        [ViewVariables]
        public readonly GoalPrototype Prototype;
        [ViewVariables]
        public readonly NpcFactionPrototype Faction;
        [ViewVariables]
        public bool Completed;
        public Goal(GoalPrototype prototype, Mind.Mind mind, NpcFactionPrototype faction, bool completed)
        {
            Prototype = prototype;
            Mind = mind;
            Faction = faction;
            Completed = completed;
        }

        public bool Equals(Goal? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(Mind, other.Mind) || !Equals(Prototype, other.Prototype) || !Equals(Faction, other.Faction)) return false;
            return true;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Goal) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Mind, Prototype, Faction);
        }
    }
}
