using Content.Server.DeltaV.Speech.Components;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using System.Text.RegularExpressions;

namespace Content.Server.DeltaV.Speech.EntitySystems;

public sealed class ScottishAccentSystem : EntitySystem
{
    [Dependency]
    private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexCh = new(@"ч", RegexOptions.IgnoreCase);
    private static readonly Regex RegexShch = new(@"щ", RegexOptions.IgnoreCase);
    private static readonly Regex RegexZh = new(@"ж", RegexOptions.IgnoreCase);
    private static readonly Regex RegexE = new(@"у", RegexOptions.IgnoreCase);
    private static readonly Regex RegexY = new(@"ы", RegexOptions.IgnoreCase);
    private static readonly Regex RegexA = new(@"а", RegexOptions.IgnoreCase);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ScottishAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, ScottishAccentComponent component)
    {
        var msg = message;
        
        msg = _replacement.ApplyReplacements(msg, "scottish");

        msg = RegexCh.Replace(msg, "тш");
        msg = RegexShch.Replace(msg, "ш");
        msg = RegexZh.Replace(msg, "дш");
        msg = RegexE.Replace(msg, "'э");
        msg = RegexY.Replace(msg, "и");
        msg = RegexA.Replace(msg, "э");

        // Добавление случайных американизмов
        var words = msg.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (Random.Shared.NextDouble() < 0.01)
            {
                words[i] += " йоу";
            }
            else if (Random.Shared.NextDouble() < 0.01)
            {
                words[i] += " мэн";
            }
        }
        msg = string.Join(" ", words);

        return msg;
    }

    private void OnAccentGet(EntityUid uid, ScottishAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
