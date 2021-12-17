using Microsoft.Win32;
using SourceCs;
using System.Diagnostics;
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

		private byte[] wynikAlgorytmu;

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

			var stoper = new Stopwatch();

			stoper.Start();

			byte[] wynik = czyAsembler ? await WywolywanieAlgorytmow.WywolajAlgorytmAsm(bitmapaTablicaBajtow, iloscWatkow) : await WywolywanieAlgorytmow.WywolajAlgorytmCpp(bitmapaTablicaBajtow, iloscWatkow);

			stoper.Stop();

			string czasWykonaniaAlgorytmu = $"Czas wykorzystany przez algorytm {(czyAsembler ? "ASM" : "C++")} na {iloscWatkow} wątkach: {stoper.Elapsed.Seconds}s {stoper.Elapsed.Milliseconds}ms";

			WykorzystanyCzasBlock.Text = czasWykonaniaAlgorytmu;

			ZapiszBitmapePrzycisk.IsEnabled = true;

			wynikAlgorytmu = wynik;

#if DEBUG
			File.WriteAllBytes("DebugOutput.bmp", wynikAlgorytmu);

			string debugText = string.Join('\n', wynikAlgorytmu);

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

				SciezkaDoPlikuBox.Text = nazwaPliku;
				sciezkaDoPliku = nazwaPliku;
				WykorzystanyCzasBlock.Text = "Tutaj pojawi się czas wykorzystany przez algorytm";
				ZapiszBitmapePrzycisk.IsEnabled = false;

				bitmapaTablicaBajtow = CzytnikPlikow.PrzeczytajBitmapeZPliku(sciezkaDoPliku);

				FiltrujBitmapePrzycisk.IsEnabled = true;
			}
		}

		private void ZapiszBitmapePrzycisk_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new SaveFileDialog()
			{
				Filter = "Bitmap files (*.bmp)|*.bmp|All files (*.*)|*.*",
				InitialDirectory = Directory.GetCurrentDirectory()
			};

			if (dialog.ShowDialog() == true)
			{
				var nazwaPliku = dialog.FileName;

				if (Path.GetExtension(nazwaPliku) != ".bmp")
				{
					nazwaPliku += ".bmp";
				}

				File.WriteAllBytes(nazwaPliku, wynikAlgorytmu);
			}
		}
	}
}