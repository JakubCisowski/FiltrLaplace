// Temat: Algorytm na bitmapie - filtrowanie Laplace (LAPL1).
// Krótki opis: Algorytm filtrujący przekazaną z dysku (za pomocą graficznego UI) bitmapę za pomocą filtru Laplace (LAPL1).
// Data wykonania projektu: 18.12.2021
// Semestr: 5
// Rok akademicki: 3
// Nazwisko autora: Cisowski
// Wersja: v1.0

using InterfejsUzytkownikaCs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SourceCs
{
	public class WywolywanieAlgorytmow
	{
		// Lista fragmentów filtrowanej bitmapy (potrzebujemy rozbić ją na fragmenty by korzystać z wątków).
		private static volatile List<WartoscZwracana> listaWartosci;

		[DllImport(@"C:\Programowanie\FiltrLaplace\x64\Release\SourceCpp.dll")]
		public static extern void NalozFiltrCpp(IntPtr wskaznikNaWejsciowaTablice, IntPtr wskaznikNaWyjsciowaTablice, int dlugoscBitmapy, int szerokoscBitmapy, int indeksStartowy, int ileIndeksowFiltrowac);

		// Podstawowa procedura która wywołuje algorytm w c++ na podanej ilości wątków oraz bitmapie reprezentowanej poprzez tablicę bajtów.
		// Procedura zwraca przefiltrowaną bitmapę w postaci tablicy bajtów.
		public static async Task<byte[]> WywolajAlgorytmCpp(byte[] bitmapaTablicaBajtow, int iloscWatkow)
		{
			// Na początku sprawdzamy szerokość bitmapy, wydzielamy z niej nagłówek (jego nie filtrujemy).
			int szerokoscBitmapy = ObliczSzerokoscBitmapy(bitmapaTablicaBajtow);
			byte[] bitmapaBezNaglowka = UsunNaglowekZBitmapy(bitmapaTablicaBajtow);

			// Dzielimy bitmapę na wątki i zapisujemy to w liście wartości zwracanych.
			InicjalizujWartosciZwracane(bitmapaBezNaglowka.Length, iloscWatkow);

			// Tworzymy listę wątków.
			var listaWatkow = new List<Thread>();

			// Inicjalizujemy indeks startowy bitmapy na 0.
			int indeksStartowy = 0;

			// Iterujemy się po wszystkich wątkach i wywołujemy na odpowiednim fragmencie algorytm.
			for (int i = 0; i < iloscWatkow; i++)
			{
				// Wybieramy fragment o indeksie 'i'.
				var wartoscZwracana = listaWartosci[i];

				// Wybieramy indeks startowy.
				int startowy = indeksStartowy;

				// Tworzymy wątek i wywołujemy na algorytm na odpowiednim fragmencie bitmapy.
				var watek = new Thread(() =>
				{
					// Tworzymy dwie tablice potrzebne do poprawnego wywołania algorytmu - część tablicy (na wyjście) oraz kopię bitmapy wejściowej (bez nagłówka).
					var czescTablicyWyjsciowej = new byte[wartoscZwracana.IloscFiltrowanychIndeksow];
					var kopiaBitmapyWejsciowej = new byte[bitmapaBezNaglowka.Length];

					// Wywołujemy algorytm na utworzonym wątku.
					unsafe
					{
						fixed (byte* wskaznikNaTabliceWejsciowa = &bitmapaBezNaglowka[0])
						fixed (byte* wskaznikNaTabliceWyjsciowa = &czescTablicyWyjsciowej[0])
						{
							// Konwertujemy byte* na IntPtr.
							var intPtrNaTabliceWejsciowa = new IntPtr(wskaznikNaTabliceWejsciowa);
							var intPtrNaTabliceWyjsciowa = new IntPtr(wskaznikNaTabliceWyjsciowa);

							// Wywołanie algorytmu.
							NalozFiltrCpp(intPtrNaTabliceWejsciowa, intPtrNaTabliceWyjsciowa, kopiaBitmapyWejsciowej.Length, szerokoscBitmapy, startowy, wartoscZwracana.IloscFiltrowanychIndeksow);

							// Kopiujemy tablicę wyjściową algorytmu do tablicy wyjściowej odpowiedniego elementu listy wartości zwracanych (fragmentów bitmapy).
							Marshal.Copy(intPtrNaTabliceWyjsciowa, wartoscZwracana.TablicaWyjsciowa, 0, wartoscZwracana.IloscFiltrowanychIndeksow);
						}
					}
				});
				watek.Start();
				listaWatkow.Add(watek);

				// Zwiększamy odpowiednio indeks startowy (przygotowanie dla następnego fragmentu).
				indeksStartowy += wartoscZwracana.IloscFiltrowanychIndeksow;
			}

			listaWatkow.ForEach(watek => watek.Join());

			// Tworzymy tablicę wyjściową - Łączymy fragmenty czyli wyniki wątków (tablice wyjściowe wartości zwracnych) by połączyć je w końcową bitmapę będącą końcowym wynikiem algorymu.
			byte[] tablicaWyjsciowa = Array.Empty<byte>();
			listaWartosci.OrderBy(wartosc => wartosc.IdWatku).ToList().ForEach(wartosc =>
			{
				tablicaWyjsciowa = tablicaWyjsciowa.Concat(wartosc.TablicaWyjsciowa).ToArray();
			});

			// Łączymy nagłówek bitmapy z jej ciałem i zwracamy wynik.
			byte[] bitmapaWyjsciowaZNaglowkiem = new byte[bitmapaTablicaBajtow.Length];
			Array.Copy(bitmapaTablicaBajtow, 0, bitmapaWyjsciowaZNaglowkiem, 0, 54);
			Array.Copy(tablicaWyjsciowa, 0, bitmapaWyjsciowaZNaglowkiem, 54, tablicaWyjsciowa.Length);
			return bitmapaWyjsciowaZNaglowkiem;
		}

		// Procedura zwracająca ciało bitmapy bez nagłówka z podanej jako argument bitmapy w postaci tablicy bajtów.
		private static byte[] UsunNaglowekZBitmapy(byte[] bitmapaTablicaBajtow)
		{
			byte[] bitmapaBezNaglowka = new byte[bitmapaTablicaBajtow.Length - 54];

			Array.Copy(bitmapaTablicaBajtow, 54, bitmapaBezNaglowka, 0, bitmapaBezNaglowka.Length);

			return bitmapaBezNaglowka;
		}

		// Zwraca szerokość podanej jako argument bitmapy w postaci tablicy bajtów.
		private static int ObliczSzerokoscBitmapy(byte[] bitmapaTablicaBajtow)
		{
			// Szerokość znajduje się na indeksach 18-21 w nagłówku.
			byte[] bajtyOznaczajaceSzerokosc = new byte[]
			{
				bitmapaTablicaBajtow[18],
				bitmapaTablicaBajtow[19],
				bitmapaTablicaBajtow[20],
				bitmapaTablicaBajtow[21]
			};

			int szerokosc = BitConverter.ToInt32(bajtyOznaczajaceSzerokosc, 0) * 3;

			return szerokosc;
		}

		[DllImport(@"C:\Programowanie\FiltrLaplace\x64\Release\DllAsm.dll")]
		public static extern void NalozFiltrAsm(IntPtr wskaznikNaWejsciowaTablice, IntPtr wskaznikNaWyjsciowaTablice, int dlugoscBitmapy, int szerokoscBitmapy, int indeksStartowy, int ileIndeksowFiltrowac);

		// Podstawowa procedura która wywołuje algorytm w asm na podanej ilości wątków oraz bitmapie reprezentowanej poprzez tablicę bajtów.
		// Procedura zwraca przefiltrowaną bitmapę w postaci tablicy bajtów.
		public static async Task<byte[]> WywolajAlgorytmAsm(byte[] bitmapaTablicaBajtow, int iloscWatkow)
		{
			// Na początku sprawdzamy szerokość bitmapy, wydzielamy z niej nagłówek (jego nie filtrujemy).
			int szerokoscBitmapy = ObliczSzerokoscBitmapy(bitmapaTablicaBajtow);
			byte[] bitmapaBezNaglowka = UsunNaglowekZBitmapy(bitmapaTablicaBajtow);

			// Dzielimy bitmapę na wątki i zapisujemy to w liście wartości zwracanych.
			InicjalizujWartosciZwracane(bitmapaBezNaglowka.Length, iloscWatkow);

			// Tworzymy listę wątków.
			var listaWatkow = new List<Thread>();

			// Inicjalizujemy indeks startowy bitmapy na 0.
			int indeksStartowy = 0;

			// Iterujemy się po wszystkich wątkach i wywołujemy na odpowiednim fragmencie algorytm.
			for (int i = 0; i < iloscWatkow; i++)
			{
				// Wybieramy fragment o indeksie 'i'.
				var wartoscZwracana = listaWartosci[i];

				// Wybieramy indeks startowy.
				int startowy = indeksStartowy;

				// Tworzymy wątek i wywołujemy na algorytm na odpowiednim fragmencie bitmapy.
				var watek = new Thread(() =>
				{
					// Tworzymy dwie tablice potrzebne do poprawnego wywołania algorytmu - część tablicy (na wyjście) oraz kopię bitmapy wejściowej (bez nagłówka).
					var czescTablicyWyjsciowej = new byte[wartoscZwracana.IloscFiltrowanychIndeksow];
					var kopiaBitmapyWejsciowej = new byte[bitmapaBezNaglowka.Length];

					Array.Copy(bitmapaBezNaglowka, 0, kopiaBitmapyWejsciowej, 0, bitmapaBezNaglowka.Length);

					// Wywołujemy algorytm na utworzonym wątku.
					unsafe
					{
						fixed (byte* wskaznikNaTabliceWejsciowa = &kopiaBitmapyWejsciowej[0])
						fixed (byte* wskaznikNaTabliceWyjsciowa = &czescTablicyWyjsciowej[0])
						{
							// Konwertujemy byte* na IntPtr.
							var intPtrNaTabliceWejsciowa = new IntPtr(wskaznikNaTabliceWejsciowa);
							var intPtrNaTabliceWyjsciowa = new IntPtr(wskaznikNaTabliceWyjsciowa);

							// Wywołanie algorytmu.
							NalozFiltrAsm(intPtrNaTabliceWejsciowa, intPtrNaTabliceWyjsciowa, kopiaBitmapyWejsciowej.Length, szerokoscBitmapy, startowy, wartoscZwracana.IloscFiltrowanychIndeksow);

							// Kopiujemy tablicę wyjściową algorytmu do tablicy wyjściowej odpowiedniego elementu listy wartości zwracanych (fragmentów bitmapy).
							Marshal.Copy(intPtrNaTabliceWyjsciowa, wartoscZwracana.TablicaWyjsciowa, 0, wartoscZwracana.IloscFiltrowanychIndeksow);
						}
					}
				});
				watek.Start();
				listaWatkow.Add(watek);

				// Zwiększamy odpowiednio indeks startowy (przygotowanie dla następnego fragmentu).
				indeksStartowy += wartoscZwracana.IloscFiltrowanychIndeksow;
			}

			listaWatkow.ForEach(watek => watek.Join());

			// Tworzymy tablicę wyjściową - Łączymy fragmenty czyli wyniki wątków (tablice wyjściowe wartości zwracnych) by połączyć je w końcową bitmapę będącą końcowym wynikiem algorymu.
			byte[] tablicaWyjsciowa = Array.Empty<byte>();
			listaWartosci.OrderBy(wartosc => wartosc.IdWatku).ToList().ForEach(wartosc =>
			  {
				  tablicaWyjsciowa = tablicaWyjsciowa.Concat(wartosc.TablicaWyjsciowa).ToArray();
			  });

			// Łączymy nagłówek bitmapy z jej ciałem i zwracamy wynik.
			byte[] bitmapaWyjsciowaZNaglowkiem = new byte[bitmapaTablicaBajtow.Length];
			Array.Copy(bitmapaTablicaBajtow, 0, bitmapaWyjsciowaZNaglowkiem, 0, 54);
			Array.Copy(tablicaWyjsciowa, 0, bitmapaWyjsciowaZNaglowkiem, 54, tablicaWyjsciowa.Length);
			return bitmapaWyjsciowaZNaglowkiem;
		}

		// Funkcja inicjalizująca wartości zwracane czyli fragmenty bitmapy które po skończeniu wątków będziemy łączyć w całość.
		private static void InicjalizujWartosciZwracane(int rozmiarTablicyWejsciowej, int iloscWatkow)
		{
			listaWartosci = new List<WartoscZwracana>();

			int x = 0;

			for (int i = 0; i < iloscWatkow; i++)
			{
				var wartosc = new WartoscZwracana()
				{
					IdWatku = i
				};

				// Wyliczenie ilości filtrowanych indeksów w danym wątku.
				int iloscFiltrowanychIndeksow;
				if (i == iloscWatkow - 1)
				{
					// Na końcu ilość to reszta które szostały.
					iloscFiltrowanychIndeksow = rozmiarTablicyWejsciowej - x;
				}
				else
				{
					// W każdym przypadku ilość indeksów to podzielony rozmiar bitmapy przez ilość wątków ale odejmujemy %3 by ilość indeksów była podzielna przez 3 (RGB).
					iloscFiltrowanychIndeksow = rozmiarTablicyWejsciowej / iloscWatkow;
					iloscFiltrowanychIndeksow -= iloscFiltrowanychIndeksow % 3;
				}

				wartosc.IloscFiltrowanychIndeksow = iloscFiltrowanychIndeksow;
				wartosc.TablicaWyjsciowa = new byte[iloscFiltrowanychIndeksow];

				x += iloscFiltrowanychIndeksow;

				// Dodajemy do listy fragmentow utworzoną wartość zwracaną.
				listaWartosci.Add(wartosc);
			}
		}
	}
}