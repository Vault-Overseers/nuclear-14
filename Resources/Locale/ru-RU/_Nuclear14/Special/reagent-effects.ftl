# guidebook reagent special effects

reagent-effect-guidebook-strength-modifier =
    { $chance ->
        [1] Изменяет
       *[other] изменяют
    } силу на { $strength } минимум на { NATURALFIXED($time, 3) } { MANY("second", $time) }
reagent-effect-guidebook-perception-modifier =
    { $chance ->
        [1] Изменяет
       *[other] изменяют
    } восприятие на { $perception } минимум на { NATURALFIXED($time, 3) } { MANY("second", $time) }
reagent-effect-guidebook-endurance-modifier =
    { $chance ->
        [1] Изменяет
       *[other] изменяют
    } выносливость на { $endurance } минимум на { NATURALFIXED($time, 3) } { MANY("second", $time) }
reagent-effect-guidebook-charisma-modifier =
    { $chance ->
        [1] Изменяет
       *[other] изменяют
    } харизму на { $charisma } минимум на { NATURALFIXED($time, 3) } { MANY("second", $time) }
reagent-effect-guidebook-intelligence-modifier =
    { $chance ->
        [1] Изменяет
       *[other] изменяют
    } интеллект на { $intelligence } минимум на { NATURALFIXED($time, 3) } { MANY("second", $time) }
reagent-effect-guidebook-agility-modifier =
    { $chance ->
        [1] Изменяет
       *[other] изменяют
    } ловкость на { $agility } минимум на { NATURALFIXED($time, 3) } { MANY("second", $time) }
reagent-effect-guidebook-luck-modifier =
    { $chance ->
        [1] Изменяет
       *[other] изменяют
    } удачу на { $luck } минимум на { NATURALFIXED($time, 3) } { MANY("second", $time) }
