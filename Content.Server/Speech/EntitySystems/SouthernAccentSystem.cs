using System;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
namespace Content.Server.Speech.EntitySystems;

public sealed class SouthernAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    
    private readonly Random _random = new Random();
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SouthernAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, SouthernAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;
        message = _replacement.ApplyReplacements(message, "southern");

        // Существующие замены
        message = Regex.Replace(message, @"\bр", "л");
        message = Regex.Replace(message, @"р", "л");
        message = Regex.Replace(message, @"([.!?])\s*", "$1 ла ");
        message = message.Replace("ч", "ць");
        message = Regex.Replace(message, @"\b(([а-яА-Я]+)(ть|чь|ти))\b", "$1ся");
        message = Regex.Replace(message, @"\bего\b", "его-го");
        message = Regex.Replace(message, @"\bэто\b", "это-то");
        message = message.Replace("ы", "и");
        message = Regex.Replace(message, @"\b(\d+)\s+([а-яА-Я]+)", "$1 штука $2");
        message = Regex.Replace(message, @"\bхорошо\b", "хао");
        message = Regex.Replace(message, @"\bспасибо\b", "сесе");
        message = Regex.Replace(message, @"\bздравствуйте\b", "ни хао");
        message = Regex.Replace(message, @"\bдо свидания\b", "цзай цзянь");
        message = Regex.Replace(message, @"\?", " ма?");

        // Новые замены и особенности
        // Замена "в" на "ф" в начале слов
        message = Regex.Replace(message, @"\bв", "ф");

        // Добавление "ару" после глаголов в прошедшем времени
        message = Regex.Replace(message, @"\b([а-яА-Я]+(?:л|ла|ло|ли))\b", "$1 ару");

        // Замена "что" на "сто"
        message = Regex.Replace(message, @"\bчто\b", "сто");

        // Добавление "ня" в конце некоторых слов
        var words = message.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 3 && _random.Next(100) < 15)
            {
                words[i] += "ня";
            }
        }
        message = string.Join(" ", words);

        // Случайное добавление слова "чайна" перед существительными (с вероятностью 10%)
        words = message.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (Regex.IsMatch(words[i], @"^[а-яА-Я]+$") && _random.Next(100) < 10)
            {
                words[i] = "чайна " + words[i];
            }
        }
        message = string.Join(" ", words);

        // Добавление "ни хао" в начало длинных предложений
        if (message.Length > 10 && !message.StartsWith("Ни хао"))
        {
            message = "Ни хао! " + message;
        }

        args.Message = message;
    }
}
