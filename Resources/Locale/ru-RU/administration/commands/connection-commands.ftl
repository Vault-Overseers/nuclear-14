## Strings for the "grant_connect_bypass" command.

cmd-grant_connect_bypass-desc = Временно разрешить пользователю обходить обычные проверки подключения.
cmd-grant_connect_bypass-help =
     Использование: grant_connect_bypass <пользователь> [продолжительность в минутах]
     Временно предоставляет пользователю возможность обходить обычные ограничения подключения.
     Обход действует только на этом игровом сервере и истечет через (по умолчанию) 1 час.
     Пользователь сможет подключаться независимо от белого списка, панического бункера или ограничения количества игроков.
cmd-grant_connect_bypass-arg-user = <пользователь>
cmd-grant_connect_bypass-arg-duration = [продолжительность в минутах]
cmd-grant_connect_bypass-invalid-args = Ожидалось 1 или 2 аргумента
cmd-grant_connect_bypass-unknown-user = Не удалось найти пользователя '{ $user }'
cmd-grant_connect_bypass-invalid-duration = Недопустимая продолжительность '{ $duration }'
cmd-grant_connect_bypass-success = Обход для пользователя '{ $user }' успешно добавлен
