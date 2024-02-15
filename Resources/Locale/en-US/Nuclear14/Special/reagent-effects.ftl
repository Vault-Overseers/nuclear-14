# guidebook reagent special effects

reagent-effect-guidebook-strength-modifier =
    { $chance ->
        [1] Modifies
        *[other] modify
    } strength by {$strength} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)}

reagent-effect-guidebook-perception-modifier =
    { $chance ->
        [1] Modifies
        *[other] modify
    } perception by {$perception} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)}

reagent-effect-guidebook-endurance-modifier =
    { $chance ->
        [1] Modifies
        *[other] modify
    } endurance by {$endurance} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)}

reagent-effect-guidebook-charisma-modifier =
    { $chance ->
        [1] Modifies
        *[other] modify
    } charisma by {$charisma} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)}

reagent-effect-guidebook-intelligence-modifier =
    { $chance ->
        [1] Modifies
        *[other] modify
    } intelligence by {$intelligence} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)}

reagent-effect-guidebook-agility-modifier =
    { $chance ->
        [1] Modifies
        *[other] modify
    } agility by {$agility} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)}

reagent-effect-guidebook-luck-modifier =
    { $chance ->
        [1] Modifies
        *[other] modify
    } luck by {$luck} for at least {NATURALFIXED($time, 3)} {MANY("second", $time)}
