// Temat: Algorytm na bitmapie - filtrowanie Laplace (LAPL1).
// Krótki opis: Algorytm filtrujący przekazaną z dysku (za pomocą graficznego UI) bitmapę za pomocą filtru Laplace (LAPL1).
// Data wykonania projektu: 18.12.2021
// Semestr: 5
// Rok akademicki: 3
// Nazwisko autora: Cisowski
// Wersja: v1.0

using Microsoft.Win32;
using SourceCs;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace InterfejsUzytkownikaCs
{
	public partial class MainWindow : Window
	{
		// Wybrana przez użytkownika ilość wątków.
		private int iloscWatkow;

		// Biblioteka wybrana przez użytkownika (asm/c++).
		private bool czyAsembler;

		// Biblioteka wybrana przez użytkownika (asm/c++).
		private string sciezkaDoPliku;

		// Przekazana przez użytkownika bitmapa reprezentowana jako tablica bajtów.
		private byte[] bitmapaTablicaBajtow;

		// Wynik algorytmu (przefiltrowana bitmapa) reprezentowana jako tablica bajtów.
		private byte[] wynikAlgorytmu;

		public MainWindow()
		{
			InitializeComponent();

			// Domyślne ustawienie wykonania algorytmu na c++.
			czyAsembler = false;
		}

		#region OBSŁUGA_CHECKBOXÓW

		// Zdarzenie obsługujące zaznaczenie przez użytkownika algorytmu cpp.
		private void WyborCppCheckbox_Zaznaczone(object sender, RoutedEventArgs e)
		{
			// Odznaczamy asm.
			WyborAsmCheckbox.IsChecked = false;
			czyAsembler = false;
		}

		// Zdarzenie obsługujące odznaczenie przez użytkownika algorytmu cpp.
		private void WyborCppCheckbox_Odznaczone(object sender, RoutedEventArgs e)
		{
			// Zaznaczamy asm.
			WyborAsmCheckbox.IsChecked = true;
			czyAsembler = true;
		}

		// Zdarzenie obsługujące zaznaczenie przez użytkownika algorytmu asm.
		private void WyborAsmCheckbox_Zaznaczone(object sender, RoutedEventArgs e)
		{
			// Odznaczamy C#.
			WyborCppCheckbox.IsChecked = false;
			czyAsembler = true;
		}

		// Zdarzenie obsługujące odznaczenie przez użytkownika algorytmu asm.
		private void WyborAsmCheckbox_Odznaczone(object sender, RoutedEventArgs e)
		{
			// Zaznaczamy C#.
			WyborCppCheckbox.IsChecked = true;
			czyAsembler = false;
		}

		#endregion OBSŁUGA_CHECKBOXÓW

		// Zdarzenie obsługujące naciśnięcie przez uzytkownika przycisku 'filtruj bitmapę'.
		// Procedura sprawdza ilość wątków, następnie wywołuje odpowiedni algorytm filtrowania, a ostatecznie wyświetla czas wykonania i zapisuje plik.
		private async void FiltrujBitmapePrzycisk_Click(object sender, RoutedEventArgs e)
		{
			iloscWatkow = int.Parse(ThreadCountBox.Text);

			// Sprawdzenie wprowadzonej ilości wątków.
			if (iloscWatkow > 64)
			{
				MessageBox.Show("Maksymalna obsługiwana ilość wątków to 64!");
				return;
			}

			if (iloscWatkow <= 0)
			{
				MessageBox.Show("Podano niedodatnią ilość wątków!");
				return;
			}

			// Stoper który mierzy czas wykonania algorytmu, niezależnie od jego implementacji.
			var stoper = new Stopwatch();
			stoper.Start();
			byte[] wynik = czyAsembler ? await WywolywanieAlgorytmow.WywolajAlgorytmAsm(bitmapaTablicaBajtow, iloscWatkow) : await WywolywanieAlgorytmow.WywolajAlgorytmCpp(bitmapaTablicaBajtow, iloscWatkow);
			stoper.Stop();

			// Wyświetlenie czasu wykonania algorytmu,.
			string czasWykonaniaAlgorytmu = $"Czas wykorzystany przez algorytm {(czyAsembler ? "ASM" : "C++")} na {iloscWatkow} wątkach: {stoper.Elapsed.Seconds}s {stoper.Elapsed.Milliseconds}ms";
			WykorzystanyCzasBlock.Text = czasWykonaniaAlgorytmu;

			// Odblokowanie przycisku zapisania wyniku filtrowania.
			ZapiszBitmapePrzycisk.IsEnabled = true;

			// Zapisanie wyniku algorytmu.
			wynikAlgorytmu = wynik;

			//#if DEBUG
			//			File.WriteAllBytes("DebugOutput.bmp", wynikAlgorytmu);
			//			string debugText = string.Join('\n', wynikAlgorytmu);
			//			File.WriteAllText("DebugText.txt", debugText);
			//#endif
		}

		// Zdarzenie obsługujące naciśnięcie przez uzytkownika przycisku 'przeglądania plików' w celu wybrania bitmapy do przefiltrowania.
		// Procedura sprawdza poprawność wybranego pliku (czy rozszerzenie pliku jest odpowiednie).
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

				// Sprawdzenie poprawności rozszerzenia pliku.
				if (Path.GetExtension(nazwaPliku) != ".bmp")
				{
					MessageBox.Show("Prosze wybrac plik z rozszerzeniem .bmp.", "Złe rozszerzenie pliku", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				SciezkaDoPlikuBox.Text = nazwaPliku;
				sciezkaDoPliku = nazwaPliku;
				WykorzystanyCzasBlock.Text = "Tutaj pojawi się czas wykorzystany przez algorytm";
				ZapiszBitmapePrzycisk.IsEnabled = false;

				// Zapisanie wybranej bitmapy.
				bitmapaTablicaBajtow = CzytnikPlikow.PrzeczytajBitmapeZPliku(sciezkaDoPliku);

				// Odblokowanie przycisku filtrowania bitmapy.
				FiltrujBitmapePrzycisk.IsEnabled = true;
			}
		}

		// Zdarzenie obsługujące naciśnięcie przez uzytkownika przycisku 'zapisania wyniku filtrowania' w celu wybrania lokalizacji i nazwy zapisanego pliku.
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

				// Zapisanie przefiltrowanej bitmapy do wybranej lokalizacji w systemie.
				File.WriteAllBytes(nazwaPliku, wynikAlgorytmu);
			}
		}
	}
}