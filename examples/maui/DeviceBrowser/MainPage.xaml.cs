namespace DeviceBrowser;

public partial class MainPage : ContentPage
{
	public MainPage(BluetoothScanner scanner)
	{
		InitializeComponent();

		BindingContext = scanner;
	}
}

