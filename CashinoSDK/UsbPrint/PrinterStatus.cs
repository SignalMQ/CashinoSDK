namespace CashinoSDK.UsbPrint;

// n=1: printer status (10 04 01)
public enum PrinterStatus
{
    Online,
    Offline,          // бит 3
    DrawerOpen,       // бит 2 (крышка кассы/ящик)
    Unknown,
}
