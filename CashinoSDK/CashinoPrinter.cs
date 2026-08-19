using CashinoSDK.Status;
using CashinoSDK.UsbPrint;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CashinoSDK;

/// <summary>
/// Класс для проверки завершения бумаги в принтере
/// </summary>
public class CashinoPrinter
{
    /// <summary>
    /// Команды отправляемые в USB-порт принтера
    /// </summary>
    static readonly byte[] QueryPrinter = [0x10, 0x04, 0x01];
    static readonly byte[] QueryOffline = [0x10, 0x04, 0x02];
    static readonly byte[] QueryError = [0x10, 0x04, 0x03];
    static readonly byte[] QueryPaper = [0x10, 0x04, 0x04];

    /// <summary>
    /// Событие, которое вызывается при обнаружении ошибки принтера
    /// </summary>
    public event EventHandler<PrinterErrorEventArgs> PrinterErrorEvent;

    /// <summary>
    /// Список USB-устройств (usbprint.sys)
    /// </summary>
    public List<string> Paths { get; private set; } = UsbPrintHelper.EnumeratePrinterDevicePaths();

    /// <summary>
    /// Текущее отслеживаемое устройство
    /// </summary>
    public string CurrentPath { get; set; }

    /// <summary>
    /// Таймаут после которого повторно запрашиваем статус принтера
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Инициализация соединения с принтером и запуск непрерывной проверки путем отправления команды
    /// </summary>
    public async Task InitializePrinterCommunicationAsync(CancellationToken cancellationToken = default)
    {
        if (Paths.Count == 0)
        {
            RaiseError(PrinterErrorCode.PrinterNotFound, "Принтер не найден!");
            return;
        } 
        else if (string.IsNullOrEmpty(CurrentPath))
            CurrentPath = Paths[0];

        while (!cancellationToken.IsCancellationRequested)
        {
            var paths = UsbPrintHelper.EnumeratePrinterDevicePaths();

            if (paths.Count == 0)
            {
                RaiseError(PrinterErrorCode.PrinterNotFound, "Принтер не найден!");
                await Task.Delay(TimeoutMilliseconds, cancellationToken);
                continue;
            }

            var device = UsbPrintHelper.Open(CurrentPath);

            byte printerStatus = await QueryStatusAsync(device, QueryPrinter);
            byte offlineStatus = await QueryStatusAsync(device, QueryOffline);
            byte errorStatus = await QueryStatusAsync(device, QueryError);
            byte paperStatus = await QueryStatusAsync(device, QueryPaper);

            var printer = StatusHelper.GetPrinterStatus(printerStatus);
            var offline = StatusHelper.GetOfflineStatus(offlineStatus);
            var errors = StatusHelper.GetErrorStatus(errorStatus);
            var paper = StatusHelper.GetPaperStatus(paperStatus);

            var errorCode = PrinterErrorCode.None;
            var messages = new List<string>();

            if (printer == PrinterStatus.Offline)
            {
                errorCode |= PrinterErrorCode.Offline;
                messages.Add("Принтер офлайн");
            }
            if (printer == PrinterStatus.DrawerOpen)
            {
                errorCode |= PrinterErrorCode.DrawerOpen;
                messages.Add("Открыт денежный ящик");
            }
            if (offline.HasFlag(OfflineStatus.CoverOpen))
            {
                errorCode |= PrinterErrorCode.CoverOpen;
                messages.Add("Открыта крышка");
            }
            if (offline.HasFlag(OfflineStatus.PaperEnd) || paper.HasFlag(PaperStatus.Empty))
            {
                errorCode |= PrinterErrorCode.PaperEnd;
                messages.Add("Нет бумаги");
            }
            if (paper.HasFlag(PaperStatus.NearEnd))
            {
                errorCode |= PrinterErrorCode.PaperNearEnd;
                messages.Add("Бумага заканчивается");
            }
            if (errors.HasFlag(ErrorStatus.CutterError))
            {
                errorCode |= PrinterErrorCode.CutterError;
                messages.Add("Ошибка ножа");
            }
            if (errors.HasFlag(ErrorStatus.HeadOverheat))
            {
                errorCode |= PrinterErrorCode.HeadOverheat;
                messages.Add("Перегрев головки");
            }
            if (errors.HasFlag(ErrorStatus.UnrecoverableError))
            {
                errorCode |= PrinterErrorCode.UnrecoverableError;
                messages.Add("Критическая ошибка принтера (аномальное напряжение)");
            }

            if (errorCode != PrinterErrorCode.None)
            {
                PrinterErrorEvent?.Invoke(this, new PrinterErrorEventArgs(
                    errorCode,
                    string.Join("; ", messages),
                    printer,
                    offline,
                    errors,
                    paper));
            }

            device.Dispose();

            // таймаут в 5 секунд, чтоб не спамить принтер
            await Task.Delay(TimeoutMilliseconds, cancellationToken);
        }
    }

    void RaiseError(PrinterErrorCode errorCode, string message) =>
        PrinterErrorEvent?.Invoke(this, new PrinterErrorEventArgs(errorCode, message));

    async Task<byte> QueryStatusAsync(FileStream dev, byte[] command)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            await dev.WriteAsync(command, 0, command.Length, cts.Token);

            var buffer = new byte[8];
            await dev.ReadAsync(buffer, 0, buffer.Length, cts.Token);
            return buffer[0];
        }
        catch (OperationCanceledException)
        {
            return 0x0;
        }
    }
}
