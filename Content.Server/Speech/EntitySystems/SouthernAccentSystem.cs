using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class SouthernAccentSystem : EntitySystem
{
    // Регулярные выражения для замены слов с учетом регистра
    private static readonly Regex RegexGood = new(@"(?<!\w)(хорошо)(?!\w)", RegexOptions.IgnoreCase);
    private static readonly Regex RegexThank = new(@"(?<!\w)(спасибо)(?!\w)", RegexOptions.IgnoreCase);
    private static readonly Regex RegexHello = new(@"(?<!\w)(привет)(?!\w)", RegexOptions.IgnoreCase);
    private static readonly Regex RegexGoodbye = new(@"(?<!\w)(пока)(?!\w)", RegexOptions.IgnoreCase);
    private static readonly Regex RegexHim = new(@"(?<!\w)(его)(?!\w)", RegexOptions.IgnoreCase);
    private static readonly Regex RegexThis = new(@"(?<!\w)(это)(?!\w)", RegexOptions.IgnoreCase);
    private static readonly Regex RegexWhat = new(@"(?<!\w)(что)(?!\w)", RegexOptions.IgnoreCase);

    // Регулярные выражения для замены букв с учетом регистра
    private static readonly Regex RegexReplaceR = new(@"р", RegexOptions.IgnoreCase);
    private static readonly Regex RegexReplaceSh = new(@"ш", RegexOptions.IgnoreCase);
    private static readonly Regex RegexReplaceY = new(@"ы", RegexOptions.IgnoreCase);
    private static readonly Regex RegexReplaceCh = new(@"ч", RegexOptions.IgnoreCase);
    private static readonly Regex RegexReplaceF = new(@"ф", RegexOptions.IgnoreCase);
    private static readonly Regex RegexReplaceTsa = new(@"тся\b", RegexOptions.IgnoreCase);

    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SouthernAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, SouthernAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // Apply replacement rules
        message = _replacement.ApplyReplacements(message, "southern");

        // Apply specific word replacements with case preservation
        message = RegexGood.Replace(message, match => PreserveCase(match.Value, "хао"));
        message = RegexThank.Replace(message, match => PreserveCase(match.Value, "сесе"));
        message = RegexHello.Replace(message, match => PreserveCase(match.Value, "ни хао"));
        message = RegexGoodbye.Replace(message, match => PreserveCase(match.Value, "цзай цзянь"));
        message = RegexHim.Replace(message, match => PreserveCase(match.Value, "ево"));
        message = RegexThis.Replace(message, match => PreserveCase(match.Value, "эта"));
        message = RegexWhat.Replace(message, match => PreserveCase(match.Value, "сто"));

        // Apply character replacements with case preservation
        message = RegexReplaceR.Replace(message, match => ReplaceWithCase(match.Value, "л"));
        message = RegexReplaceSh.Replace(message, match => ReplaceWithCase(match.Value, "с"));
        message = RegexReplaceY.Replace(message, match => ReplaceWithCase(match.Value, "и"));
        message = RegexReplaceCh.Replace(message, match => ReplaceWithCase(match.Value, "ць"));
        message = RegexReplaceF.Replace(message, match => ReplaceWithCase(match.Value, "в"));
        message = RegexReplaceTsa.Replace(message, match => ReplaceWithCase(match.Value, "ться"));

        args.Message = message;
    }

    private string PreserveCase(string original, string replacement)
    {
        return original.ToUpper() == original
            ? replacement.ToUpper()
            : original.ToLower() == original
                ? replacement.ToLower()
                : replacement;
    }

    private string ReplaceWithCase(string original, string replacement)
    {
        return original.ToUpper() == original
            ? replacement.ToUpper()
            : original.ToLower() == original
                ? replacement.ToLower()
                : replacement;
    }
}
