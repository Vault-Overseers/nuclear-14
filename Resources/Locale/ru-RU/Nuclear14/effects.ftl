reagent-effect-guidebook-strength-modifier =
    { $chance ->
        [1] Изменяет
       *[other] Изменяет
    } силу на { $strength } как минимум на { NATURALFIXED($time, 3) } { MANY("секунд", $time) }
