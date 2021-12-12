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
		[DllImport(@"C:\Programowanie\FiltrLaplace\x64\Debug\SourceCpp.dll")]
		public static extern void NalozFiltrCpp(IntPtr wskaznikNaWejsciowaTablice, IntPtr wskaznikNaWyjsciowaTablice, int dlugoscBitmapy, int szerokoscBitmapy, int indeksStartowy, int ileIndeksowFiltrowac);

		private static volatile List<WartoscZwracana> listaWartosci;

		public static async Task<byte[]> WywolajAlgorytmCpp(byte[] bitmapaTablicaBajtow, int iloscWatkow)
		{
			int szerokoscBitmapy = ObliczSzerokoscBitmapy(bitmapaTablicaBajtow);
			byte[] bitmapaBezNaglowka = UsunNaglowekZBitmapy(bitmapaTablicaBajtow);

			listaWartosci = await InicjalizujAlgorytm(bitmapaBezNaglowka, iloscWatkow, szerokoscBitmapy, false);

			var listaWatkow = new List<Thread>();

			int indeksStartowy = 0;

			for (int i = 0; i < iloscWatkow; i++)
			{
				var wartoscZwracana = listaWartosci[i];

				int startowy = indeksStartowy;

				var watek = new Thread(() =>
				{
					var czescTablicyWyjsciowej = new byte[wartoscZwracana.IloscFiltrowanychIndeksow];
					var kopiaBitmapyWejsciowej = new byte[bitmapaBezNaglowka.Length];
					Array.Copy(bitmapaBezNaglowka, 0, kopiaBitmapyWejsciowej, 0, bitmapaBezNaglowka.Length);

					unsafe
					{
						fixed (byte* wskaznikNaTabliceWejsciowa = &kopiaBitmapyWejsciowej[0])
						fixed (byte* wskaznikNaTabliceWyjsciowa = &czescTablicyWyjsciowej[0])
						{
							var intPtrNaTabliceWejsciowa = new IntPtr(wskaznikNaTabliceWejsciowa);
							var intPtrNaTabliceWyjsciowa = new IntPtr(wskaznikNaTabliceWyjsciowa);

							NalozFiltrCpp(intPtrNaTabliceWejsciowa, intPtrNaTabliceWyjsciowa, kopiaBitmapyWejsciowej.Length, szerokoscBitmapy, startowy, wartoscZwracana.IloscFiltrowanychIndeksow);

							Marshal.Copy(intPtrNaTabliceWyjsciowa, wartoscZwracana.TablicaWyjsciowa, 0, wartoscZwracana.IloscFiltrowanychIndeksow);
						}
					}

				});

				watek.Start();

				listaWatkow.Add(watek);

				indeksStartowy += wartoscZwracana.IloscFiltrowanychIndeksow;
			}

			listaWatkow.ForEach(watek => watek.Join());

			byte[] tablicaWyjsciowa = Array.Empty<byte>();

			listaWartosci.ForEach(wartosc =>
			{
				tablicaWyjsciowa = tablicaWyjsciowa.Concat(wartosc.TablicaWyjsciowa).ToArray();
			});

			byte[] bitmapaWyjsciowaZNaglowkiem = new byte[bitmapaTablicaBajtow.Length];

			Array.Copy(bitmapaTablicaBajtow, 0, bitmapaWyjsciowaZNaglowkiem, 0, 54);
			Array.Copy(tablicaWyjsciowa, 0, bitmapaWyjsciowaZNaglowkiem, 54, tablicaWyjsciowa.Length);

			return bitmapaWyjsciowaZNaglowkiem;
		}

		private static byte[] UsunNaglowekZBitmapy(byte[] bitmapaTablicaBajtow)
		{
			byte[] bitmapaBezNaglowka = new byte[bitmapaTablicaBajtow.Length - 54];

			Array.Copy(bitmapaTablicaBajtow, 54, bitmapaBezNaglowka, 0, bitmapaBezNaglowka.Length);

			return bitmapaBezNaglowka;
		}

		private static int ObliczSzerokoscBitmapy(byte[] bitmapaTablicaBajtow)
		{
			// Szerokość znajduje się na indeksach 18-21.
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

		[DllImport(@"C:\Programowanie\FiltrLaplace\x64\Debug\DllAsm.dll")]
		public static extern void NalozFiltrAsm(IntPtr wskaznikNaWejsciowaTablice, IntPtr wskaznikNaWyjsciowaTablice, int dlugoscBitmapy, int szerokoscBitmapy, int indeksStartowy, int ileIndeksowFiltrowac);

		public static async Task<byte[]> WywolajAlgorytmAsm(byte[] bitmapaTablicaBajtow, int iloscWatkow)
		{
			int szerokoscBitmapy = ObliczSzerokoscBitmapy(bitmapaTablicaBajtow);
			byte[] bitmapaBezNaglowka = UsunNaglowekZBitmapy(bitmapaTablicaBajtow);

			listaWartosci = await InicjalizujAlgorytm(bitmapaBezNaglowka, iloscWatkow, szerokoscBitmapy, true);

			var listaWatkow = new List<Thread>();

			int indeksStartowy = 0;

			for (int i = 0; i < iloscWatkow; i++)
			{
				var wartoscZwracana = listaWartosci[i];

				int startowy = indeksStartowy;

				var watek = new Thread(() =>
				{
					var czescTablicyWyjsciowej = new byte[wartoscZwracana.IloscFiltrowanychIndeksow];
					var kopiaBitmapyWejsciowej = new byte[bitmapaBezNaglowka.Length];
					Array.Copy(bitmapaBezNaglowka, 0, kopiaBitmapyWejsciowej, 0, bitmapaBezNaglowka.Length);

					unsafe
					{
						fixed (byte* wskaznikNaTabliceWejsciowa = &kopiaBitmapyWejsciowej[0])
						fixed (byte* wskaznikNaTabliceWyjsciowa = &czescTablicyWyjsciowej[0])
						{
							var intPtrNaTabliceWejsciowa = new IntPtr(wskaznikNaTabliceWejsciowa);
							var intPtrNaTabliceWyjsciowa = new IntPtr(wskaznikNaTabliceWyjsciowa);

							NalozFiltrAsm(intPtrNaTabliceWejsciowa, intPtrNaTabliceWyjsciowa, kopiaBitmapyWejsciowej.Length, szerokoscBitmapy, startowy, wartoscZwracana.IloscFiltrowanychIndeksow);

							Marshal.Copy(intPtrNaTabliceWyjsciowa, wartoscZwracana.TablicaWyjsciowa, 0, wartoscZwracana.IloscFiltrowanychIndeksow);
						}
					}

				});

				watek.Start();

				listaWatkow.Add(watek);

				indeksStartowy += wartoscZwracana.IloscFiltrowanychIndeksow;
			}

			listaWatkow.ForEach(watek => watek.Join());

			byte[] tablicaWyjsciowa = Array.Empty<byte>();

			listaWartosci.OrderBy(wartosc=>wartosc.IdWatku).ToList().ForEach(wartosc =>
			{
				tablicaWyjsciowa = tablicaWyjsciowa.Concat(wartosc.TablicaWyjsciowa).ToArray();
			});

			byte[] bitmapaWyjsciowaZNaglowkiem = new byte[bitmapaTablicaBajtow.Length];

			Array.Copy(bitmapaTablicaBajtow, 0, bitmapaWyjsciowaZNaglowkiem, 0, 54);
			Array.Copy(tablicaWyjsciowa, 0, bitmapaWyjsciowaZNaglowkiem, 54, tablicaWyjsciowa.Length);

			return bitmapaWyjsciowaZNaglowkiem;
		}

		public static async Task<List<WartoscZwracana>> InicjalizujAlgorytm(byte[] bitmapaTablicaBajtow, int iloscWatkow, int szerokoscBitmapy, bool czyAsembler)
		{
			var wartosciZwracane = new List<WartoscZwracana>();

			int x = 0;

			for (int i = 0; i < iloscWatkow; i++)
			{
				var wartosc = new WartoscZwracana()
				{
					IdWatku = i
				};

				int iloscFiltrowanychIndeksow;

				if (i == iloscWatkow - 1)
				{
					iloscFiltrowanychIndeksow = bitmapaTablicaBajtow.Length - x;
				}
				else
				{
					iloscFiltrowanychIndeksow = bitmapaTablicaBajtow.Length / iloscWatkow;
					iloscFiltrowanychIndeksow -= iloscFiltrowanychIndeksow % 3;
				}

				wartosc.IloscFiltrowanychIndeksow = iloscFiltrowanychIndeksow;
				wartosc.TablicaWyjsciowa = new byte[iloscFiltrowanychIndeksow];

				x += iloscFiltrowanychIndeksow;

				wartosciZwracane.Add(wartosc);
			}

			var listaWatkow = new List<Thread>();

			int indeksStartowy = 0;

			for (int i = 0; i < iloscWatkow; i++)
			{
				var wartoscZwracana = wartosciZwracane[i];

				int startowy = indeksStartowy;

				var watek = new Thread(() =>
				{
					bool czyAsm = czyAsembler;
					var czescTablicyWyjsciowej = new byte[wartoscZwracana.IloscFiltrowanychIndeksow];
					var kopiaBitmapyWejsciowej = new byte[bitmapaTablicaBajtow.Length];
					Array.Copy(bitmapaTablicaBajtow, 0, kopiaBitmapyWejsciowej, 0, bitmapaTablicaBajtow.Length);

					unsafe
					{
						fixed (byte* wskaznikNaTabliceWejsciowa = &kopiaBitmapyWejsciowej[0])
						fixed (byte* wskaznikNaTabliceWyjsciowa = &czescTablicyWyjsciowej[0])
						{
							var intPtrNaTabliceWejsciowa = new IntPtr(wskaznikNaTabliceWejsciowa);
							var intPtrNaTabliceWyjsciowa = new IntPtr(wskaznikNaTabliceWyjsciowa);

							if (czyAsm)
							{
								NalozFiltrAsm(intPtrNaTabliceWejsciowa, intPtrNaTabliceWyjsciowa, kopiaBitmapyWejsciowej.Length, szerokoscBitmapy, startowy, wartoscZwracana.IloscFiltrowanychIndeksow);
							}
							else
							{
								NalozFiltrCpp(intPtrNaTabliceWejsciowa, intPtrNaTabliceWyjsciowa, kopiaBitmapyWejsciowej.Length, szerokoscBitmapy, startowy, wartoscZwracana.IloscFiltrowanychIndeksow);
							}

							Marshal.Copy(intPtrNaTabliceWyjsciowa, wartoscZwracana.TablicaWyjsciowa, 0, wartoscZwracana.IloscFiltrowanychIndeksow);
						}
					}

				});

				watek.Start();

				listaWatkow.Add(watek);

				indeksStartowy += wartoscZwracana.IloscFiltrowanychIndeksow;
			}

			listaWatkow.ForEach(watek => watek.Join());

			return wartosciZwracane;
		}
	}
}