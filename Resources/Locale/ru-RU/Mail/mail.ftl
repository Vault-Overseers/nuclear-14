mail-recipient-mismatch = Имя получателя или должность не совпадают.
mail-invalid-access = Имя получателя и должность совпадают, но доступ не соответствует ожиданиям.
mail-locked = Анти-взломный замок не снят. Нажмите на ID получателя.
mail-desc-far = Посылка почты. На расстоянии не разглядеть, кому она адресована.
mail-desc-close = Посылка почты, адресованная { CAPITALIZE($name) }, { $job }.
mail-desc-fragile = На посылке есть [color=red]красная метка "хрупкое"[/color].
mail-desc-priority = [color=yellow]Жёлтая метка "приоритет"[/color] на анти-взломном замке активирована. Лучше доставить в срок!
mail-desc-priority-inactive = [color=#886600]Жёлтая метка "приоритет"[/color] на анти-взломном замке неактивна.
mail-unlocked = Анти-взломная система разблокирована.
mail-unlocked-by-emag = Анти-взломная система *BZZT* разблокирована.
mail-unlocked-reward = Анти-взломная система разблокирована. { $bounty } спесосов добавлено на счёт логистики.
mail-penalty-lock = АНТИ-ВЗЛОМНЫЙ ЗАМОК СЛОМАН. СЧЁТ ЛОГИСТИКИ ПОНИЖЕН НА { $credits } КРЕДИТОВ.
mail-penalty-fragile = НАРУШЕНА ЦЕЛОСТНОСТЬ. СЧЁТ ЛОГИСТИКИ ПОНИЖЕН НА { $credits } КРЕДИТОВ.
mail-penalty-expired = ПРОСРОЧЕННАЯ ДОСТАВКА. СЧЁТ ЛОГИСТИКИ ПОНИЖЕН НА { $credits } КРЕДИТОВ.
mail-item-name-unaddressed = почта
mail-item-name-addressed = почта ({ $recipient })
command-mailto-description = Поместить посылку для доставки получателю. Пример использования: `mailto 1234 5678 false false`. Содержимое целевого контейнера будет перенесено в настоящую почтовую посылку.
command-mailto-help = Использование: { $command } <идентификатор получателя> <идентификатор контейнера> [хрупкое: true или false] [приоритетное: true или false]
command-mailto-no-mailreceiver = Целевой получатель не имеет { $requiredComponent }.
command-mailto-no-blankmail = Прототип { $blankMail } не существует. Что-то пошло не так. Обратитесь к программисту.
command-mailto-bogus-mail = У { $blankMail } отсутствует { $requiredMailComponent }. Что-то пошло не так. Обратитесь к программисту.
command-mailto-invalid-container = Целевой контейнер не имеет { $requiredContainer } контейнера.
command-mailto-unable-to-receive = Невозможно настроить целевого получателя для приёма почты. Возможно, отсутствует идентификатор.
command-mailto-no-teleporter-found = Не удалось связать целевого получателя с телепортёром почты на станции. Получатель может быть вне станции.
command-mailto-success = Успех! Почтовая посылка поставлена в очередь для следующей телепортации через { $timeToTeleport } секунд.
command-mailnow = Принудительно вызвать все телепортёры почты для мгновенной доставки ещё одной партии почты. Это не пропустит лимит недоставленной почты.
command-mailnow-help = Использование: { $command }
command-mailnow-success = Успех! Все телепортёры почты скоро доставят ещё одну партию почты.
