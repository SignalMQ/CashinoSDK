namespace CashinoSDK.Status;

internal static class StatusHelper
{
    public static PrinterStatus GetPrinterStatus(byte s) => s switch
    {
        _ when (s & 0x12) != 0x12 => PrinterStatus.Unknown,
        _ when (s & 0x08) == 0x08 => PrinterStatus.Offline,     // бит 3 -> off-line
        _ when (s & 0x04) == 0x04 => PrinterStatus.DrawerOpen,  // бит 2 -> ящик открыт
        _ => PrinterStatus.Online,
    };

    public static OfflineStatus GetOfflineStatus(byte s)
    {
        if ((s & 0x12) != 0x12) return OfflineStatus.Invalid;

        var result = OfflineStatus.None;
        if ((s & 0x04) == 0x04) result |= OfflineStatus.CoverOpen;
        if ((s & 0x08) == 0x08) result |= OfflineStatus.FeedButton;
        if ((s & 0x20) == 0x20) result |= OfflineStatus.PaperEnd;
        if ((s & 0x40) == 0x40) result |= OfflineStatus.Error;
        return result;
    }

    public static ErrorStatus GetErrorStatus(byte s)
    {
        if ((s & 0x12) != 0x12) return ErrorStatus.Invalid;

        var result = ErrorStatus.None;
        if ((s & 0x08) == 0x08) result |= ErrorStatus.CutterError;
        if ((s & 0x20) == 0x20) result |= ErrorStatus.UnrecoverableError;
        if ((s & 0x40) == 0x40) result |= ErrorStatus.HeadOverheat;
        return result;
    }

    public static PaperStatus GetPaperStatus(byte s) => s switch
    {
        _ when (s & 0x12) != 0x12 => PaperStatus.Unknown,
        _ when (s & 0x60) == 0x60 => PaperStatus.Empty,    // биты 5,6 -> paper end
        _ when (s & 0x0C) == 0x0C => PaperStatus.NearEnd,  // биты 2,3 -> near-end
        _ => PaperStatus.Ok,
    };
}
