using System.Text.RegularExpressions;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Server.Nuclear14.Special.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Nuclear14.Special.Speech.EntitySystems
{
    public sealed class LowIntelligenceAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        private static readonly Regex yesRegex = new(@"(?:yes\b|yep\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex noRegex = new(@"(?:no\b|nope\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ingRegex = new(@"(?:ing\b|in\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ilityRegex = new(@"(?:ility\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex tionRegex = new(@"(?:tion\b|tions\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex lettersRegex = new(@"[ao]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // corvax localisation
        private static readonly Regex daRegex = new(@"(?:да\b|ага\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex netRegex = new(@"(?:нет\b|неа\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ingRussianRegex = new(@"(?:ить\b|ать\b|ять\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ostRegex = new(@"(?:ость\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex aciaRegex = new(@"(?:ация\b|ации\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex russianLettersRegex = new(@"[оае]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex pronounRegex = new(@"\b(я|ты|он|она|оно|мы|вы|они)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex adjectiveEndingRegex = new(@"(ый|ая|ое|ые|ого|ому|ым|ом|ую|ой|ем|их|им|ими)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex verbEndingRegex = new(@"(ет|ут|ют|ит|ат|ят|ел|ла|ло|ли|ешь|ишь|ем|им|ете|ите|ать|ять|уть|ыть)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex complexWordsRegex = new(@"\b\w{7,}\b", RegexOptions.Compiled);

        public override void Initialize()
        {
            SubscribeLocalEvent<LowIntelligenceAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            message = message.Trim();
            message = yesRegex.Replace(message, "duh");
            message = noRegex.Replace(message, "nungh");
            message = ingRegex.Replace(message, "uuuh");
            message = ilityRegex.Replace(message, "uuity");
            message = tionRegex.Replace(message, "tuun");
            message = lettersRegex.Replace(message, "u");

            // corvax localisation
            message = daRegex.Replace(message, "ду");
            message = netRegex.Replace(message, "нунх");
            message = ingRussianRegex.Replace(message, "ууу");
            message = ostRegex.Replace(message, "усть");
            message = aciaRegex.Replace(message, "уция");
            message = russianLettersRegex.Replace(message, "у");
            message = pronounRegex.Replace(message, "мя");
            message = adjectiveEndingRegex.Replace(message, "ый");
            message = verbEndingRegex.Replace(message, "ыт");
            message = complexWordsRegex.Replace(message, m => "у" + new string('м', _random.Next(2, 5)));
            message = message.Replace("что", "чу").Replace("как", "кук").Replace("где", "гды").Replace("когда", "кугду");
            message = Regex.Replace(message, @"[.!?]", m => new string(m.Value[0], _random.Next(1, 4)));
            message = Regex.Replace(message, @"\b\w+\b", m => _random.Prob(0.2f) ? m.Value.ToUpper() : m.Value);

            return message;
        }

        private void OnAccent(EntityUid uid, LowIntelligenceAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
