namespace DeviceBrowser;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using triaxis.BluetoothLE;

public class BluetoothDeviceCollection : ObservableCollection<BluetoothDevice>
{
    public void ProcessAdvertisement(IAdvertisement advertisement)
    {
        var dev = this.FirstOrDefault(d => d.Peripheral == advertisement.Peripheral);

        if (dev == null)
        {
            // add a new device
            dev = new BluetoothDevice(advertisement.Peripheral);
            dev.Update(advertisement);
            Add(dev);
        }
        else if (dev.Update(advertisement))
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, dev, dev, IndexOf(dev)));
        }
    }
}
