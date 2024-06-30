using System.Linq;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Shared._NC14.DayNightCycle
{
    public sealed class DayNightCycleSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private const float EARLY_MORNING_TIME = 0.2f; // This represents 20% into the cycle, which is early morning

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DayNightCycleComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<DayNightCycleComponent, ComponentStartup>(OnComponentStartup);
        }

        private void OnMapInit(EntityUid uid, DayNightCycleComponent component, MapInitEvent args)
        {
            if (component.TimeEntries.Count < 2)
            {
                // Default Fallout-inspired color cycle with more variants
                component.TimeEntries = new List<TimeEntry>
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
            }

            InitializeEarlyMorning(component);
        }

        private void OnComponentStartup(EntityUid uid, DayNightCycleComponent component, ComponentStartup args)
        {
            InitializeEarlyMorning(component);
        }

        private void InitializeEarlyMorning(DayNightCycleComponent component)
        {
            component.CurrentCycleTime = EARLY_MORNING_TIME;
            UpdateLightColor(component);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<DayNightCycleComponent, MapLightComponent>();
            while (query.MoveNext(out var uid, out var dayNight, out var mapLight))
            {
                dayNight.CurrentCycleTime += frameTime / (dayNight.CycleDurationMinutes * 60f);
                dayNight.CurrentCycleTime %= 1f; // Keep it between 0 and 1

                UpdateLightColor(dayNight, mapLight, uid);
            }
        }

        private void UpdateLightColor(DayNightCycleComponent dayNight, MapLightComponent? mapLight = null, EntityUid? uid = null)
        {
            var color = GetInterpolatedColor(dayNight);

            if (mapLight != null && uid.HasValue)
            {
                mapLight.AmbientLightColor = color;
                Dirty(uid.Value, mapLight);
                Dirty(uid.Value, dayNight);
            }
        }

        private Color GetInterpolatedColor(DayNightCycleComponent component)
        {
            var entries = component.TimeEntries;
            var time = component.CurrentCycleTime;

            for (int i = 0; i < entries.Count - 1; i++)
            {
                if (time >= entries[i].Time && time <= entries[i + 1].Time)
                {
                    var t = (time - entries[i].Time) / (entries[i + 1].Time - entries[i].Time);
                    return InterpolateHexColors(entries[i].ColorHex, entries[i + 1].ColorHex, t);
                }
            }

            // If we're here, we're between the last and first entry
            var lastEntry = entries.Last();
            var firstEntry = entries.First();
            var wrappedT = (time - lastEntry.Time) / (1f + firstEntry.Time - lastEntry.Time);
            return InterpolateHexColors(lastEntry.ColorHex, firstEntry.ColorHex, wrappedT);
        }

        private Color InterpolateHexColors(string hexColor1, string hexColor2, float t)
        {
            Color color1 = Color.FromHex(hexColor1);
            Color color2 = Color.FromHex(hexColor2);

            float r = color1.R + (color2.R - color1.R) * t;
            float g = color1.G + (color2.G - color1.G) * t;
            float b = color1.B + (color2.B - color1.B) * t;

            return new Color(r, g, b);
        }
    }
}