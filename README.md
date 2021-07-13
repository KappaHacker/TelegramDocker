# TelegramBotWithPostgreSqlOnDocker

## About
TelegramBotWithPostgreSqlOnDocker - бот, который выполняет функцию отслеживания номера миграции в telegram чате.

Функции: вывод текущего номера миграции, установление следующего номера миграции путем инкрементирования текущего номера и установка нового номера миграции.

## Test bot locally

### BotFather
Нужно создать бота через BotFather и получить токен. Все настройки отображения бота и команд происходят через него.

### PostgreSql
Необходимо установить PostgreSql и настроить его. Скачать нужную [версию - download](https://www.postgresql.org/download).
Также установите необходимые Nuget пакеты EF core  для взаимодействия c базой.

### Set a token
Сначала вам нужно установить собственный токен в переменной окружения. Для этого укажите токен, который принадлежит вашему боту.
Также будет необходимо заменить строку подключения к PostgreSql и путь до JSON файла.

#### ProgramSettings.cs
```
public static class Configuration
    {
        public readonly static string conectionString = "Host=localhost;Port=5432;Database=MigrationTelegramBot;Username=*****;Password=*****";
        public readonly static string path = "gg.json";
        public readonly static string BotToken = "******************************************";
    }
```

### Docker
Чтобы упаковать проект в docker необходимо будет внести изменения в пути до ваших файлов.


