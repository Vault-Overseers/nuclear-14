character-age-requirement =
    Вам { $inverted ->
        [true] не следует быть
       *[other] следует быть
    } в возрасте от [color=yellow]{ $min }[/color] до [color=yellow]{ $max }[/color] лет
character-species-requirement =
    Вам { $inverted ->
        [true] не следует быть
       *[other] следует быть
    } [color=green]{ $species }[/color]
character-trait-requirement =
    Вам { $inverted ->
        [true] не следует иметь
       *[other] следует иметь
} одну из черт: [color=lightblue]{$traits}[/color]
character-backpack-type-requirement =
    Вам { $inverted ->
        [true] не следует использовать
       *[other] следует использовать
    } [color=lightblue]{ $type }[/color] в качестве вашей сумки
character-clothing-preference-requirement =
    Вам { $inverted ->
        [true] не следует носить
       *[other] следует носить
    } [color=lightblue]{ $type }[/color]
character-job-requirement =
    Вы должны { $inverted ->
        [true] не быть
       *[other] быть
    } одним из этих профессий: { $jobs }
character-department-requirement =
    Вы должны { $inverted ->
        [true] не быть
       *[other] быть
    } в одном из этих отделов: { $departments }
character-timer-department-insufficient = Вам необходимо [color=yellow]{ TOSTRING($time, "0") }[/color] минут дополнительно в отделе [color={ $departmentColor }]{ $department }[/color]
character-timer-department-too-high = Вам необходимо [color=yellow]{ TOSTRING($time, "0") }[/color] минут меньше в отделе [color={ $departmentColor }]{ $department }[/color]
character-timer-overall-insufficient = Вам необходимо [color=yellow]{ TOSTRING($time, "0") }[/color] минут дополнительно общего времени игры
character-timer-overall-too-high = Вам необходимо [color=yellow]{ TOSTRING($time, "0") }[/color] минут меньше общего времени игры
character-timer-role-insufficient = Вам необходимо [color=yellow]{ TOSTRING($time, "0") }[/color] минут дополнительно в роли [color={ $departmentColor }]{ $job }[/color]
character-timer-role-too-high = Вам необходимо [color=yellow]{ TOSTRING($time, "0") }[/color] минут меньше в роли [color={ $departmentColor }]{ $job }[/color]
character-trait-group-exclusion-requirement = Вы не можете выбрать это, если у вас есть одна из следующих черт: { $traits }
character-loadout-group-exclusion-requirement = Вы не можете выбрать это, если у вас есть один из следующих наборов снаряжения: { $loadouts }
