namespace DeviceBrowser;

using System.Text;
using triaxis.BluetoothLE;

public class BluetoothDevice
{
    public BluetoothDevice(IPeripheral peripheral)
    {
        Peripheral = peripheral;
    }

    public IPeripheral Peripheral { get; }

    public string? DeviceName { get; private set; }
    public int Rssi { get; private set; }
#if ANDROID
    public string Address => Peripheral.Uuid.RightHalf.ToString("X12");
#else
    public string Address => Peripheral.Uuid.ToString();
#endif

    internal bool Update(IAdvertisement advertisement)
    {
        var rawName = advertisement[AdvertisementRecord.CompleteLocalName];
        var newName = rawName == null ? DeviceName : Encoding.ASCII.GetString(rawName);
        var newRssi = advertisement.Rssi;
        bool change = DeviceName != newName || Rssi != newRssi;
        DeviceName = newName;
        Rssi = newRssi;
        return change;
    }
}
