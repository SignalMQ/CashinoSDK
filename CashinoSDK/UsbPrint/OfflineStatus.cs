using System;

namespace CashinoSDK.UsbPrint;

// n=2: off-line status (10 04 02) — здесь несколько причин могут быть одновременно,
// поэтому [Flags], а не одиночное состояние
[Flags]
public enum OfflineStatus
{
    None = 0,
    CoverOpen = 1 << 0,  // бит 2 (0x04)
    FeedButton = 1 << 1,  // бит 3 (0x08) — нажата кнопка feed
    PaperEnd = 1 << 2,  // бит 5 (0x20) — нет бумаги
    Error = 1 << 3,  // бит 6 (0x40)
    Invalid = 1 << 7,  // не похоже на валидный ответ
}
