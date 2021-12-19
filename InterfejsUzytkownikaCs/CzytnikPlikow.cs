// Temat: Algorytm na bitmapie - filtrowanie Laplace (LAPL1).
// Krótki opis: Algorytm filtrujący przekazaną z dysku (za pomocą graficznego UI) bitmapę za pomocą filtru Laplace (LAPL1).
// Data wykonania projektu: 18.12.2021
// Semestr: 5
// Rok akademicki: 3
// Nazwisko autora: Cisowski
// Wersja: v1.0

using System.IO;

namespace InterfejsUzytkownikaCs
{
	public static class CzytnikPlikow
	{
		// Konwertuje bitmapę z podanej lokalizacji w systemie na tablicę bajtów.
		public static byte[] PrzeczytajBitmapeZPliku(string sciezka)
		{
			byte[] bitmapa = File.ReadAllBytes(sciezka);

			return bitmapa;
		}
	}
}