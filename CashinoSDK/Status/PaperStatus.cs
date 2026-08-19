namespace CashinoSDK.Status;

// n=4: paper sensor status (10 04 04)
public enum PaperStatus
{
    Ok,        // бумага в норме
    NearEnd,   // заканчивается (биты 2,3)
    Empty,     // закончилась (биты 5,6)
    Unknown,   // байт не похож на валидный ответ
}
