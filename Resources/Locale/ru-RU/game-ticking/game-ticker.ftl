game-ticker-restart-round = Перемотка времени...
game-ticker-start-round = Новый день в Пустоши начинается...
game-ticker-start-round-cannot-start-game-mode-fallback = Не удалось активировать протокол выживания { $failedGameMode }! Активируем резервный протокол { $fallbackMode }...
game-ticker-start-round-cannot-start-game-mode-restart = Не удалось активировать протокол выживания { $failedGameMode }! Перезагрузка симуляции...
game-ticker-start-round-invalid-map = Выбранная территория { $map } не совместима с протоколом выживания { $mode }. Возможны сбои в работе Пип-Боя...
game-ticker-unknown-role = Таинственный незнакомец
game-ticker-delay-start = Запуск симуляции отложен на { $seconds } секунд. Проверьте свое снаряжение.
game-ticker-pause-start = Симуляция приостановлена. Воспользуйтесь моментом, чтобы перевести дух.
game-ticker-pause-start-resumed = Симуляция возобновлена. Приготовьтесь к выживанию.
game-ticker-player-join-game-message = Добро пожаловать в Corvax Fallout! Новичок? Нажми ESC и изучи Кодекс Выживания. Нужна помощь? Используй радиосигнал "SOS" (Админ помощь).
game-ticker-get-info-text =
    Привет и добро пожаловать на [color=white]Corvax Fallout![/color]
    День выживания: [color=white]#{ $roundId }[/color]
    Выживших в округе: [color=white]{ $playerCount }[/color]
    Территория: [color=white]{ $mapName }[/color]
    Условия выживания: [color=white]{ $gmTitle }[/color]
    >[color=yellow]{ $desc }[/color]
game-ticker-get-info-preround-text =
    Привет и добро пожаловать на [color=white]Corvax Fallout![/color]
    День выживания: [color=white]#{ $roundId }[/color]
    Выживших в округе: [color=white]{ $playerCount }[/color] ([color=white]{ $readyCount }[/color] { $readyCount ->
        [one] готов
       *[other] готовы
    })
    Территория: [color=white]{ $mapName }[/color]
    Условия выживания: [color=white]{ $gmTitle }[/color]
    >[color=yellow]{ $desc }[/color]
game-ticker-no-map-selected = [color=red]Внимание! Зона выживания не определена![/color]
game-ticker-player-no-jobs-available-when-joining = При попытке войти в симуляцию все роли были заняты. Попробуйте позже или выберите другую специализацию.
game-ticker-welcome-to-the-station = Добро пожаловать в пустоши, выживальщик. Удачи и держи свой счётчик Гейгера под рукой!
# Displayed in chat to admins when a player joins
player-join-message = Выживший { $name } появился на горизонте!
player-first-join-message = Новичок { $name } впервые вышел из убежища.
# Displayed in chat to admins when a player leaves
player-leave-message = Выживший { $name } исчез в пустошах!
latejoin-arrival-announcement = Внимание всем! Обнаружен новый выживший в секторе.
latejoin-arrival-sender = Радиовещание
latejoin-arrivals-direction = Радиоактивная буря грядёт...
latejoin-arrivals-direction-time = Радиоактивная буря прибудет через... { $time }.
preset-not-enough-ready-players = Не удалось активировать протокол { $presetName }. Требуется минимум { $minimumPlayers } выживших, но готовы только { $readyPlayersCount }.
preset-no-one-ready = Не удалось активировать протокол { $presetName }. Нет готовых выживших.
