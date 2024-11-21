using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC14.DayNightCycle
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class DayNightCycleComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("cycleDuration")]
        public float CycleDurationMinutes { get; set; } = 60f; // Default cycle duration is 60 minutes

        [DataField("timeEntries")]
        public List<TimeEntry> TimeEntries { get; set; } = new()
        {
            new() { Time = 0.00f, ColorHex = "#000000" }, // Midnight
            new() { Time = 0.04f, ColorHex = "#02020b" }, // Very early morning
            new() { Time = 0.08f, ColorHex = "#312716" }, // Early dawn
            new() { Time = 0.17f, ColorHex = "#4E3D23" }, // Dawn
            new() { Time = 0.25f, ColorHex = "#58372D" }, // Sunrise
            new() { Time = 0.33f, ColorHex = "#876A42" }, // Early morning
            new() { Time = 0.42f, ColorHex = "#A08042" }, // Mid-morning
            new() { Time = 0.50f, ColorHex = "#A88F73" }, // Noon
            new() { Time = 0.58f, ColorHex = "#C1A78A" }, // Early afternoon
            new() { Time = 0.67f, ColorHex = "#7D6244" }, // Late afternoon
            new() { Time = 0.75f, ColorHex = "#8C6130" }, // Sunset
            new() { Time = 0.83f, ColorHex = "#543521" }, // Dusk
            new() { Time = 0.92f, ColorHex = "#02020b" }, // Early night
            new() { Time = 1.00f, ColorHex = "#000000" }  // Back to Midnight
        };

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public int CurrentTimeEntryIndex { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public float CurrentCycleTime { get; set; }
    }

    [DataDefinition, NetSerializable, Serializable]
    public sealed partial class TimeEntry
    {
        [DataField("colorHex")]
        public string ColorHex { get; set; } = "#FFFFFF";

        [DataField("time")]
        public float Time { get; set; } // Normalized time (0-1)
    }
}