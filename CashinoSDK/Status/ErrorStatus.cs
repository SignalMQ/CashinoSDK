using System;

namespace CashinoSDK.Status;

// n=3: error status (10 04 03) — тоже несколько ошибок могут быть сразу
[Flags]
public enum ErrorStatus
{
    None = 0,
    CutterError = 1 << 0,  // бит 3 (0x08)
    UnrecoverableError = 1 << 1,  // бит 5 (0x20) — аномальное напряжение и т.п.
    HeadOverheat = 1 << 2,  // бит 6 (0x40) — перегрев/напряжение головки
    Invalid = 1 << 7,
}
