using Microsoft.Win32;
using SourceCs;
using System.IO;
using System.Windows;

namespace InterfejsUzytkownikaCs
{
	public partial class MainWindow : Window
	{
		private int iloscWatkow;
		private bool czyAsembler;

		private string sciezkaDoPliku;

		private byte[] bitmapaTablicaBajtow;

		public MainWindow()
		{
			InitializeComponent();
			czyAsembler = false;
		}

		#region OBSŁUGA_CHECKBOXÓW

		private void WyborCppCheckbox_Zaznaczone(object sender, RoutedEventArgs e)
		{
			// Odznaczamy asm.
			WyborAsmCheckbox.IsChecked = false;
			czyAsembler = false;
		}

		private void WyborCppCheckbox_Odznaczone(object sender, RoutedEventArgs e)
		{
			// Zaznaczamy asm.
			WyborAsmCheckbox.IsChecked = true;
			czyAsembler = true;
		}

		private void WyborAsmCheckbox_Zaznaczone(object sender, RoutedEventArgs e)
		{
			// Odznaczamy C#.
			WyborCppCheckbox.IsChecked = false;
			czyAsembler = true;
		}

		private void WyborAsmCheckbox_Odznaczone(object sender, RoutedEventArgs e)
		{
			// Zaznaczamy C#.
			WyborCppCheckbox.IsChecked = true;
			czyAsembler = false;
		}

		#endregion OBSŁUGA_CHECKBOXÓW

		private async void FiltrujBitmapePrzycisk_Click(object sender, RoutedEventArgs e)
		{
			iloscWatkow = int.Parse(ThreadCountBox.Text);

			if (iloscWatkow > 64)
			{
				MessageBox.Show("Maksymalna obsługiwana ilość wątków to 64 !");
				return;
			}

			if (iloscWatkow <= 0)
			{
				MessageBox.Show("Podano niedodatnią ilość wątków!");
				return;
			}

			byte[] wynik = czyAsembler ? await WywolywanieAlgorytmow.WywolajAlgorytmAsm(bitmapaTablicaBajtow, iloscWatkow) : await WywolywanieAlgorytmow.WywolajAlgorytmCpp(bitmapaTablicaBajtow, iloscWatkow);

#if DEBUG
			File.WriteAllBytes("DebugOutput.bmp", wynik);

			string debugText = string.Join('\n', wynik);

			File.WriteAllText("DebugText.txt", debugText);
#endif
		}

		private void PrzegladajPlikiPrzycisk_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog()
			{
				Filter = "Bitmap files (*.bmp)|*.bmp|All files (*.*)|*.*",
				InitialDirectory = Directory.GetCurrentDirectory()
			};

			if (dialog.ShowDialog() == true)
			{
				var nazwaPliku = dialog.FileName;

				if (Path.GetExtension(nazwaPliku) != ".bmp")
				{
					MessageBox.Show("Prosze wybrac plik z rozszerzeniem .bmp.", "Złe rozszerzenie pliku", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				BitmapyPanel.Children.Clear();
				SciezkaDoPlikuBox.Text = nazwaPliku;
				sciezkaDoPliku = nazwaPliku;

				bitmapaTablicaBajtow = CzytnikPlikow.PrzeczytajBitmapeZPliku(sciezkaDoPliku);

				FiltrujBitmapePrzycisk.IsEnabled = true;
			}
		}
	}
}