# TurFirm

TurFirm — информационная система турфирмы. Проект предназначен для автоматизации
основных процессов работы туристического агентства: приёма заявок на формирование
туров, подбора и согласования маршрутов с клиентом, ведения истории туров клиентов.

Приложение состоит из **веб-клиента на Blazor (Interactive Server)** для клиентов
и работников турфирмы, **REST API на ASP.NET Core** и локальной базы данных **SQLite**.

## Возможности

В проекте реализованы следующие основные возможности:

**Для клиента:**

* регистрация и вход в систему;
* создание заявки на формирование тура (направление, длительность в днях, пожелания);
* просмотр своих заявок и их статусов;
* просмотр предложений от работников по каждой заявке — с построчным составом услуг
  (название, описание, цена) и итоговой стоимостью тура;
* принятие или отклонение предложения;
* просмотр раздела «Мои туры» — список туров с итоговой ценой и раскрывающимся
  составом услуг.

**Для работника турфирмы:**

* вход в систему (при первом запуске API автоматически создаётся учётная запись
  по умолчанию `employer` / `password`);
* просмотр активных заявок;
* отправка клиенту предложений с сопроводительным текстом и позициями-услугами
  (название, описание, цена);
* добавление тура клиенту, в том числе при очном обращении;
* просмотр списка всех клиентов;
* редактирование туров;
* экспорт данных в форматах **JSON / CSV / XML / YAML**.

## Структура проекта

```text
TurFirm/
└── Turfirm/
    ├── Turfirm.csproj            # REST API (ASP.NET Core, .NET 10) — точка входа API
    ├── Program.cs                # запуск API: контроллеры, Swagger, CORS, инициализация БД
    ├── TourFirm.db               # база данных SQLite (создаётся при первом запуске)
    ├── Properties/
    │   └── launchSettings.json   # https://localhost:5001, launchUrl: swagger
    │
    ├── docs/
    │   └── diagrams/             # диаграммы PlantUML
    │       ├── usecase.puml
    │       ├── component.puml
    │       ├── er_diagram.puml
    │       ├── er_diagram.png
    │       └── sequence.pulm
    │
    ├── src/
    │   ├── Api/                  # REST-контроллеры
    │   │   ├── API.cs            # UsersController — auth, заявки, предложения, туры
    │   │   └── ExportController.cs
    │   ├── Data/                 # слой данных и модели
    │   │   ├── DataBaseControl.cs# доступ к SQLite через Microsoft.Data.Sqlite (ADO.NET)
    │   │   └── Users.cs          # модели предметной области
    │   ├── Export/               # экспорт данных
    │   │   └── DataExporter.cs   # JSON / CSV / XML / YAML
    │   └── Web/                  # веб-клиент на Blazor
    │       ├── Web.csproj        # Blazor Web App (Interactive Server), .NET 10
    │       ├── Program.cs        # регистрация HttpClient, UserSession, Razor Components
    │       ├── appsettings.json
    │       ├── Properties/launchSettings.json
    │       ├── wwwroot/          # app.css, статические ассеты
    │       └── Components/
    │           ├── App.razor
    │           ├── _Imports.razor
    │           ├── Layout/       # MainLayout.razor, ...
    │           ├── Pages/        # Login, Register, ClientDashboard, EmployeeDashboard,
    │           │                 # Export, NotFound
    │           └── Services/     # UserSession.cs, ClientDtos.cs
    │
    └── tests/                    # зарезервировано под автотесты (пока пусто)
```

## Архитектура

Приложение построено по клиент-серверной схеме с разделением на веб-клиент и API.
Пользовательский интерфейс не обращается к базе данных напрямую — только через
REST API по HTTPS/JSON.

```text
           Браузер
              │
              ▼
   Blazor Web App (Interactive Server)      ← src/Web
   рендеринг на сервере, SignalR-circuit
              │
              ▼  HTTPS / JSON
   REST API (ASP.NET Core, Swagger)         ← Turfirm (корневой проект)
              │
              ▼
   Слой доступа к данным (ADO.NET,          ← src/Data
   Microsoft.Data.Sqlite)
              │
              ▼
   SQLite — TourFirm.db
```

Такое разделение позволяет независимо развивать клиент и сервер: REST API можно
переиспользовать для другого клиента (например, мобильного) без переписывания
бизнес-логики и схемы базы данных.

### Основные компоненты

#### `Turfirm` (корневой проект, `Turfirm.csproj`)

ASP.NET Core Web-проект, который одновременно:

* хостит **REST API** (`AddControllers`, `MapControllers`) и **Swagger UI**
  (`Swashbuckle.AspNetCore`);
* содержит **слой данных** (`src/Data`) — работу с SQLite через `Microsoft.Data.Sqlite`
  (ADO.NET: ручные SQL-запросы, без EF Core и миграций);
* содержит **доменные модели** (`src/Data/Users.cs`): `UserData`, `NewTourRequest`,
  `NewProposal`, `ProposalItemDto`, `NewTourData`, `UpdateTourData` и др.;
* содержит **сервис экспорта** (`src/Export/DataExporter.cs`) и `ExportController`;
* при первом запуске, если `TourFirm.db` отсутствует, создаёт схему базы данных
  (`GenerateDataBase()`) и заводит учётную запись работника по умолчанию.

Из компиляции корневого проекта исключён каталог `src/Web`
(`<Compile Remove="src\Web\**" />`), чтобы исходный код веб-клиента не попадал в
сборку API.

#### `src/Web` (`Web.csproj`)

**Blazor Web App** с режимом `InteractiveServer` (`@rendermode InteractiveServer`).
Содержит страницы для клиента и работника, а также:

* `UserSession` — состояние текущего пользователя в пределах circuit (Scoped-сервис);
* `ClientDtos.cs` — лёгкие DTO для десериализации ответов API;
* единую точку обращений к API — зарегистрированный в DI `HttpClient` с базовым
  адресом `https://localhost:5001/`.

### Схема базы данных

База `TourFirm.db` содержит таблицы:

* `Offices` — офисы турфирмы;
* `Users` — клиенты и сотрудники (`Role`: 0 — клиент, 1 — сотрудник);
* `TourRequests` — заявки клиентов на тур;
* `TourProposals` — предложения от сотрудников по заявкам;
* `ProposalItems` — позиции (услуги) внутри предложения с ценой;
* `Tours` — туры, привязанные к клиенту (и опционально к заявке).

Стоимость тура вычисляется как сумма цен позиций предложения, которое клиент
согласовал, — отдельного поля «цена» у тура нет.

## Требования

Для сборки и запуска проекта необходимо установить:

* **.NET SDK 10.0** или выше (проекты target `net10.0`);
* Visual Studio 2022 (Community) с workload'ом **ASP.NET and web development** —
  опционально, для запуска из CLI достаточно `dotnet`;
* Git (опционально).

> База данных создаётся автоматически при первом запуске API — инструменты EF Core
> и миграции **не требуются**.

## Установка

### 1. Получение исходников

```bash
git clone <url-репозитория> TourFirm
cd TourFirm/Turfirm
```

> Файл решения `.sln` в репозитории отсутствует — сборка и запуск выполняются
> по отдельным проектам (см. ниже).

### 2. Восстановление зависимостей

```bash
dotnet restore Turfirm.csproj
dotnet restore src/Web/Web.csproj
```

### 3. Создание базы данных

Отдельный шаг не нужен: при первом запуске API (см. раздел «Запуск приложения»)
файл `TourFirm.db` создаётся рядом с `Turfirm.csproj`, в нём разворачивается схема
таблиц и заводится учётная запись работника `employer` / `password`.

## Запуск приложения

Для работы нужны **два** запущенных процесса одновременно — REST API и веб-клиент.

**Терминал 1 — REST API** (из каталога `Turfirm/`):

```bash
dotnet run --project Turfirm.csproj
```

API поднимется на `https://localhost:5001` (HTTP — `http://localhost:5000`).
Swagger UI: `https://localhost:5001/swagger`.

**Терминал 2 — веб-клиент Blazor** (из каталога `Turfirm/`):

```bash
dotnet run --project src/Web/Web.csproj
```

Веб-клиент запускается на `https://localhost:7133` (HTTP — `http://localhost:5298`);
точный адрес `dotnet` выведет в консоль. Откройте его в браузере — попадёте на
страницу входа.

В Visual Studio: настройте **Multiple startup projects**, выставьте действие
**Start** у проектов `Turfirm` и `src/Web/Web.csproj`, затем нажмите **F5**.

> В режиме разработки API использует самоподписанный dev-сертификат. Веб-клиент в
> окружении `Development` доверяет любому сертификату API
> (`DangerousAcceptAnyServerCertificateValidator`) — это **только для разработки**;
> в продакшене настройте валидацию сертификатов и HTTPS надлежащим образом.

## Экспорт данных

Выгрузка данных в форматы **JSON / CSV / XML / YAML** реализована в
`src/Export/DataExporter.cs` и доступна через REST API (`src/Api/ExportController.cs`)
и через страницу `/export` в веб-клиенте (только для сотрудников).

Главная таблица экспорта — `TourRequests` со связанными `TourProposals` →
`ProposalItems`. При запуске рядом с API создаётся папка `out/` с четырьмя файлами:
`data.json`, `data.csv`, `data.xml`, `data.yaml`.

| Действие | Метод |
| --- | --- |
| Запустить выгрузку (только сотрудник) | `POST /api/export/run?userId=<id сотрудника>` |
| Скачать готовый файл | `GET /api/export/download/{json\|csv\|xml\|yaml}` |

Через сайт: страница **«Экспорт данных»** (`/export`) — кнопка запуска и ссылки на
скачивание всех четырёх форматов.

## Используемые технологии

* C# / .NET 10
* ASP.NET Core Web API (контроллеры) + Swagger (Swashbuckle.AspNetCore)
* Blazor Web App, режим **Interactive Server** (SignalR)
* Microsoft.Data.Sqlite (ADO.NET) — доступ к данным
* SQLite
* YamlDotNet — экспорт в YAML
* PlantUML — диаграммы; Markdown — документация

> В `Turfirm.csproj` также подключены пакеты EF Core (`Microsoft.EntityFrameworkCore.Sqlite`,
> `Microsoft.EntityFrameworkCore.Design`) и `BCrypt.Net-Next` — они зарезервированы на
> будущее. В текущем коде слой данных использует «голый» ADO.NET, а проверка пароля —
> прямое сравнение строк (без хеширования).

## Разработка

Исходный код располагается в каталогах `src/`. При разработке рекомендуется соблюдать
следующую организацию кода:

* модели предметной области — в `src/Data` (`Users.cs`);
* работу с базой данных — в `src/Data` (`DataBaseControl.cs`);
* HTTP-контроллеры и бизнес-логику — в `src/Api`;
* сервис экспорта — в `src/Export`;
* код интерфейса — в `src/Web/Components` (`Pages`, `Layout`, `Services`);
* обращения к API — через `HttpClient` из DI веб-клиента и DTO из
  `src/Web/Components/Services`.

### Работа с Git

Проект использует модель ветвления **feature branch workflow**: основная ветка
`main` всегда содержит стабильную версию, новые функции разрабатываются в отдельных
ветках `feature/*` и вливаются через Pull Request.

Типичный цикл:

```bash
git switch -c feature/название-задачи
# ... изменения ...
git add .
git commit -m "Краткое описание"
git push -u origin feature/название-задачи
```

## Диаграммы

Диаграммы проекта построены с использованием PlantUML и расположены в каталоге
`docs/diagrams/`:

* `usecase.puml` — варианты использования системы;
* `component.puml` — компонентная архитектура;
* `er_diagram.puml` (+ `er_diagram.png`) — схема базы данных;
* `sequence.pulm` — последовательность взаимодействия при создании заявки.

## Лицензия

Проект является учебным. Условия использования могут быть определены в файле `LICENSE`.
