<<<<<<<< HEAD:Content.Shared/GameTicking/Components/EndedGameRuleComponent.cs
﻿namespace Content.Shared.GameTicking.Components;
========
﻿namespace Content.Server.GameTicking.Components;
>>>>>>>> master:Content.Server/GameTicking/Components/EndedGameRuleComponent.cs

/// <summary>
///     Added to game rules before <see cref="GameRuleEndedEvent"/>.
///     Mutually exclusive with <seealso cref="ActiveGameRuleComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class EndedGameRuleComponent : Component;
