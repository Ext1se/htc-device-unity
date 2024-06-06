namespace VIVE_Trackers
{
    // pair state:
    // 0xA000005 - приложение работает, деваайс находится в режиме привязки
    // 0x3000005 - приложение прекратило работу, но деваайс привязан
    // 0x4000003 - unpaired, pairing info present?
    // 0x1000003 - unpaired, pairing info not present?
    // 0x32000008 - paired and Online
    // 0x320fc008 - paired
    // 0x320ff808 - paired
    // 0x320fa808 - paired and Offline (power off)
    // 0x320D5008 - на некоторое время показал такой статус после вызова команды poweroff
    // 0x3201B008 - то же самое, только на другом трекере
    // 0x320F9008 - то же самое, только на другом трекере
    // 0x3208E008 - то же самое, только на другом трекере
    public enum PairState : int // 32000008
    {
        ReadyForScan = 0x04000003,  // unpaired, pairing info present?,
        PairedIdle = 0x03000005,  // приложение прекратило работу, но девайс привязан
        PairedLocked = 0x0A000005,  // приложение работает, деваайс находится в режиме привязки
        UnpairedNoInfo = 0x01000003,  // unpaired, pairing info not present?
        Paired0 = 0x320FC008, // paired
        Paired1 = 0x320FF808, // paired
        Paired2 = 0x320FA808, // paired
        RequiredSetup = 0x32000008,
        Offline = 0x320FA008  // paired?
    }
}
