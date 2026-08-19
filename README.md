# CashinoSDK

CashinoSDK - .NET Standard 2.0 библиотека для мониторинга USB-принтеров Cashino, работающих через драйвер `usbprint.sys` в Windows.

SDK отправляет ESC/POS real-time status команды в USB-порт принтера, регулярно считывает состояние устройства и сообщает об ошибках через событие `PrinterErrorEvent`.

## Возможности

- Поиск подключенных USB-принтеров через Windows device interface `GUID_DEVINTERFACE_USBPRINT`.
- Опрос статусов принтера, офлайн-состояний, ошибок и датчика бумаги.
- Уведомления о нескольких ошибках одновременно через флаговый `PrinterErrorCode`.
- Настраиваемый интервал опроса.
- Поддержка отмены фонового мониторинга через `CancellationToken`.

## Требования

- Windows.
- .NET SDK для сборки проекта.
- Приложение-потребитель, совместимое с `netstandard2.0`.
- USB-принтер, доступный через `usbprint.sys`.

## Подключение

Добавьте проект SDK как ссылку в ваше приложение:

```powershell
dotnet add <YourApp.csproj> reference .\CashinoSDK\CashinoSDK.csproj
```

Или подключите собранную библиотеку `CashinoSDK.dll` из каталога публикации.

## Быстрый старт

```csharp
using CashinoSDK;
using CashinoSDK.Status;

var printer = new CashinoPrinter
{
    TimeoutMilliseconds = 5000
};

printer.PrinterErrorEvent += (_, args) =>
{
    Console.WriteLine($"Printer error: {args.ErrorCode}");
    Console.WriteLine(args.Message);

    if (args.ErrorCode.HasFlag(PrinterErrorCode.PaperEnd))
    {
        Console.WriteLine("Replace paper roll.");
    }
};

using var cts = new CancellationTokenSource();

await printer.InitializePrinterCommunicationAsync(cts.Token);
```

`InitializePrinterCommunicationAsync` запускает непрерывный цикл опроса. Метод завершится после отмены переданного `CancellationToken`.

## Выбор принтера

При создании `CashinoPrinter` свойство `Paths` заполняется списком найденных USB-принтеров. Если `CurrentPath` не задан, SDK использует первый найденный путь.

```csharp
var printer = new CashinoPrinter();

foreach (var path in printer.Paths)
{
    Console.WriteLine(path);
}

printer.CurrentPath = printer.Paths[0];
```

Если принтер не найден, SDK вызовет `PrinterErrorEvent` с кодом `PrinterErrorCode.PrinterNotFound`.

## Обрабатываемые ошибки

`PrinterErrorCode` является `[Flags]`, поэтому одно событие может содержать несколько состояний:

| Код | Описание |
| --- | --- |
| `PrinterNotFound` | USB-принтер не найден. |
| `Offline` | Принтер находится в офлайн-состоянии. |
| `DrawerOpen` | Открыт денежный ящик. |
| `CoverOpen` | Открыта крышка принтера. |
| `PaperNearEnd` | Бумага заканчивается. |
| `PaperEnd` | Бумага закончилась. |
| `CutterError` | Ошибка ножа. |
| `HeadOverheat` | Перегрев печатающей головки. |
| `UnrecoverableError` | Критическая ошибка принтера, например аномальное напряжение. |

В `PrinterErrorEventArgs` также доступны низкоуровневые статусы:

- `PrinterStatus`
- `OfflineStatus`
- `ErrorStatus`
- `PaperStatus`

## Настройка интервала опроса

По умолчанию принтер опрашивается каждые 5000 мс:

```csharp
printer.TimeoutMilliseconds = 3000;
```

Не устанавливайте слишком маленький интервал, чтобы не создавать лишнюю нагрузку на устройство.

## Сборка

```powershell
dotnet build
```

Публикация библиотеки:

```powershell
dotnet publish .\CashinoSDK\CashinoSDK.csproj -c Release -r win-x64
```

## Лицензия

См. [LICENSE.txt](LICENSE.txt).
