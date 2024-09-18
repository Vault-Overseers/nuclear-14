<<<<<<<< HEAD:Content.Shared/GameTicking/Components/EndedGameRuleComponent.cs
﻿namespace Content.Shared.GameTicking.Components;
========
﻿namespace Content.Server.GameTicking.Components;
>>>>>>>> 8dc036c8a7 (Upstream merge 16th Sept 2024 (#527)):Content.Server/GameTicking/Components/EndedGameRuleComponent.cs

/// <summary>
///     Added to game rules before <see cref="GameRuleEndedEvent"/>.
///     Mutually exclusive with <seealso cref="ActiveGameRuleComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class EndedGameRuleComponent : Component;
