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
    /// Событие, которое вызывается при обнаружении ошибки принтера
    /// </summary>
    public event EventHandler PrinterErrorEvent;

    /// <summary>
    /// Команды отправляемые в USB-порт принтера
    /// </summary>
    static readonly byte[] QueryPrinter = { 0x10, 0x04, 0x01 };
    static readonly byte[] QueryOffline = { 0x10, 0x04, 0x02 };
    static readonly byte[] QueryError = { 0x10, 0x04, 0x03 };
    static readonly byte[] QueryPaper = { 0x10, 0x04, 0x04 };

    /// <summary>
    /// Список USB-устройств (принтеров)
    /// </summary>
    readonly List<string> _paths = UsbPrintHelper.EnumeratePrinterDevicePaths();

    /// <summary>
    /// Таймаут после которого повторно запрашиваем статус принтера повторно
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Инициализация соединения с принтером и запуск непрерывной проверки путем отправления команды
    /// </summary>
    public async void InitializePrinterCommunication()
    {
        if (_paths.Count == 0)
        {
            throw new Exception("Принтер не найден!");
        }

        while (true)
        {
            var paths = UsbPrintHelper.EnumeratePrinterDevicePaths();

            if (paths.Count == 0)
            {
                PrinterErrorEvent?.Invoke(this, EventArgs.Empty);
                await Task.Delay(TimeoutMilliseconds);
                continue;
            }

            var device = UsbPrintHelper.Open(paths[0]);

            byte printerStatus = await QueryStatusAsync(device, QueryPrinter);
            byte offlineStatus = await QueryStatusAsync(device, QueryOffline);
            byte errorStatus = await QueryStatusAsync(device, QueryError);
            byte paperStatus = await QueryStatusAsync(device, QueryPaper);

            var printer = StatusHelper.GetPrinterStatus(printerStatus);
            var offline = StatusHelper.GetOfflineStatus(offlineStatus);
            var errors = StatusHelper.GetErrorStatus(errorStatus);
            var paper = StatusHelper.GetPaperStatus(paperStatus);

            if (offline.HasFlag(OfflineStatus.PaperEnd)) throw new Exception("Нет бумаги");
            if (offline.HasFlag(OfflineStatus.CoverOpen)) throw new Exception("Открыта крышка");
            if (errors.HasFlag(ErrorStatus.CutterError)) throw new Exception("Ошибка ножа");
            if (errors.HasFlag(ErrorStatus.HeadOverheat)) throw new Exception("Перегрев головки");
            if (paper.HasFlag(PaperStatus.Empty) || paper.HasFlag(PaperStatus.NearEnd)) throw new Exception("Нет бумаги");

            if (printer == PrinterStatus.Offline ||
                errors != ErrorStatus.None ||
                offline != OfflineStatus.None ||
                paper != PaperStatus.Ok)
                PrinterErrorEvent?.Invoke(this, EventArgs.Empty);

            device.Dispose();

            // таймаут в 5 секунд, чтоб не спамить принтер
            await Task.Delay(TimeoutMilliseconds);
        }
    }

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
