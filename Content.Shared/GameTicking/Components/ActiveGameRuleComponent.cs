<<<<<<<< HEAD:Content.Shared/GameTicking/Components/ActiveGameRuleComponent.cs
﻿namespace Content.Shared.GameTicking.Components;
========
﻿namespace Content.Server.GameTicking.Components;
>>>>>>>> master:Content.Server/GameTicking/Components/ActiveGameRuleComponent.cs

/// <summary>
///     Added to game rules before <see cref="GameRuleStartedEvent"/> and removed before <see cref="GameRuleEndedEvent"/>.
///     Mutually exclusive with <seealso cref="EndedGameRuleComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveGameRuleComponent : Component;
