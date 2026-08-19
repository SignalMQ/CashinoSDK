using System;

namespace CashinoSDK.Status;

/// <summary>
/// Данные события об ошибке принтера
/// </summary>
public class PrinterErrorEventArgs(
    PrinterErrorCode errorCode,
    string message,
    PrinterStatus printerStatus = PrinterStatus.Unknown,
    OfflineStatus offlineStatus = OfflineStatus.None,
    ErrorStatus errorStatus = ErrorStatus.None,
    PaperStatus paperStatus = PaperStatus.Unknown) : EventArgs
{
    /// <summary>
    /// Обнаруженные типы ошибок (может быть несколько одновременно)
    /// </summary>
    public PrinterErrorCode ErrorCode { get; } = errorCode;

    /// <summary>
    /// Человекочитаемое описание ошибки (может содержать несколько причин через "; ")
    /// </summary>
    public string Message { get; } = message;

    public PrinterStatus PrinterStatus { get; } = printerStatus;

    public OfflineStatus OfflineStatus { get; } = offlineStatus;

    public ErrorStatus ErrorStatus { get; } = errorStatus;

    public PaperStatus PaperStatus { get; } = paperStatus;
}

