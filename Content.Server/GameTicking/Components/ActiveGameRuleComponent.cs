<<<<<<<< HEAD:Content.Shared/GameTicking/Components/ActiveGameRuleComponent.cs
﻿namespace Content.Shared.GameTicking.Components;
========
﻿namespace Content.Server.GameTicking.Components;
>>>>>>>> 8dc036c8a7 (Upstream merge 16th Sept 2024 (#527)):Content.Server/GameTicking/Components/ActiveGameRuleComponent.cs

/// <summary>
///     Added to game rules before <see cref="GameRuleStartedEvent"/> and removed before <see cref="GameRuleEndedEvent"/>.
///     Mutually exclusive with <seealso cref="EndedGameRuleComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveGameRuleComponent : Component;
