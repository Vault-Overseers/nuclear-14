using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Damage.Systems;

public sealed class DamageExamineSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(EntityUid uid, DamageExaminableComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var ev = new DamageExamineEvent(new FormattedMessage(), args.User);
        RaiseLocalEvent(uid, ref ev);
        if (!ev.Message.IsEmpty)
        {
            _examine.AddDetailedExamineVerb(args, component, ev.Message,
                Loc.GetString("damage-examinable-verb-text"),
                "/Textures/Interface/VerbIcons/smite.svg.192dpi.png",
                Loc.GetString("damage-examinable-verb-message")
            );
        }
    }

    public void AddDamageExamine(FormattedMessage message, DamageSpecifier damageSpecifier, string? type = null,
        float ignoreCoefficients = 0f, bool isGunDamage = false)
    {
        var markup = new FormattedMessage();
        if (isGunDamage)
        {
            markup = GetGunDamageExamine(damageSpecifier, type);
        }
        else
        {
            markup = GetDamageExamine(damageSpecifier, type, ignoreCoefficients);
        }

        if (!message.IsEmpty)
        {
            message.PushNewline();
        }
        message.AddMessage(markup);
    }

    /// <summary>
    /// Retrieves the damage examine values.
    /// </summary>
    private FormattedMessage GetDamageExamine(DamageSpecifier damageSpecifier, string? type = null,
        float ignoreCoefficients = 0f)
    {
        var msg = new FormattedMessage();

        if (string.IsNullOrEmpty(type))
        {
            if (ignoreCoefficients != 0f)
            {
                msg.PushNewline();
                msg.AddMarkup(Loc.GetString("damage-examine-armor-piercing", ("ignore", MathF.Round((1f - ignoreCoefficients) * 100, 1))));
            }
            msg.AddMarkup(Loc.GetString("damage-examine"));
        }
        else
        {
            if (ignoreCoefficients != 0f)
            {
                msg.PushNewline();
                msg.AddMarkup(Loc.GetString("damage-examine-type-armor-piercing", ("type", type),
                    ("ignore", MathF.Round((1f - ignoreCoefficients) * 100, 1))));
            }
            msg.AddMarkup(Loc.GetString("damage-examine-type", ("type", type)));
        }

        foreach (var damage in damageSpecifier.DamageDict)
        {
            if (damage.Value != FixedPoint2.Zero)
            {
                msg.PushNewline();
                msg.AddMarkup(Loc.GetString("damage-value", ("type", damage.Key), ("amount", damage.Value)));
            }
        }

        return msg;
    }
    private FormattedMessage GetGunDamageExamine(DamageSpecifier damageSpecifier, string? type = null)
    {
        var msg = new FormattedMessage();

        msg.AddMarkup(Loc.GetString("damage-gun-examine"));

        foreach (var damage in damageSpecifier.DamageDict)
        {
            if (damage.Value != FixedPoint2.Zero)
            {
                msg.PushNewline();
                if (damage.Value > 0)
                    msg.AddMarkup(Loc.GetString("damage-gun-examine-enhances", ("type", damage.Key), ("amount", damage.Value)));
                if (damage.Value < 0)
                    msg.AddMarkup(Loc.GetString("damage-gun-examine-weakens", ("type", damage.Key), ("amount", damage.Value)));
            }
        }

        return msg;
    }
}
