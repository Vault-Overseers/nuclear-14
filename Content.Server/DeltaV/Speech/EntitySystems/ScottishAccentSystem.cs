using Content.Server.DeltaV.Speech.Components;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using System.Text.RegularExpressions;
using System.Linq;

namespace Content.Server.DeltaV.Speech.EntitySystems;

public sealed class ScottishAccentSystem : EntitySystem
{
    [Dependency]
    private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexCh = new(@"ч", RegexOptions.IgnoreCase);
    private static readonly Regex RegexShch = new(@"щ", RegexOptions.IgnoreCase);
    private static readonly Regex RegexZh = new(@"ж", RegexOptions.IgnoreCase);
    private static readonly Regex RegexE = new(@"е", RegexOptions.IgnoreCase);
    private static readonly Regex RegexY = new(@"ы", RegexOptions.IgnoreCase);
    private static readonly Regex RegexA = new(@"а", RegexOptions.IgnoreCase);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ScottishAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, ScottishAccentComponent component)
    {
        var words = message.Split(' ');
        var accentuatedWords = new List<string>();

        foreach (var word in words)
        {
            // Применяем замены из словаря
            var accentuatedWord = _replacement.ApplyReplacements(word, "scottish");

            // Если слово не было заменено словарем, применяем регулярные выражения
            if (accentuatedWord == word)
            {
                accentuatedWord = ApplyRegexReplacements(accentuatedWord);
            }

            // Добавление случайных американизмов
            if (Random.Shared.NextDouble() < 0.01)
            {
                accentuatedWord += " йоу";
            }
            else if (Random.Shared.NextDouble() < 0.01)
            {
                accentuatedWord += " мэн";
            }

            accentuatedWords.Add(accentuatedWord);
        }

        return string.Join(" ", accentuatedWords);
    }

    private string ApplyRegexReplacements(string word)
    {
        word = RegexCh.Replace(word, "тш");
        word = RegexShch.Replace(word, "ш");
        word = RegexZh.Replace(word, "дш");
        word = RegexE.Replace(word, "'э");
        word = RegexY.Replace(word, "и");
        word = RegexA.Replace(word, "э");
        return word;
    }

    private void OnAccentGet(EntityUid uid, ScottishAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
