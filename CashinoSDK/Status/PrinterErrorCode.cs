using System;

namespace CashinoSDK.Status;

/// <summary>
/// Типы ошибок принтера, передаваемые пользователю через <see cref="PrinterErrorEventArgs"/>
/// </summary>
[Flags]
public enum PrinterErrorCode
{
    None = 0,
    PrinterNotFound = 1 << 0,
    Offline = 1 << 1,
    DrawerOpen = 1 << 2,
    CoverOpen = 1 << 3,
    PaperNearEnd = 1 << 4,
    PaperEnd = 1 << 5,
    CutterError = 1 << 6,
    HeadOverheat = 1 << 7,
    UnrecoverableError = 1 << 8,
}

