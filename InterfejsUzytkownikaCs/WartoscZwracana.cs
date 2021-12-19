// Temat: Algorytm na bitmapie - filtrowanie Laplace (LAPL1).
// Krótki opis: Algorytm filtrujący przekazaną z dysku (za pomocą graficznego UI) bitmapę za pomocą filtru Laplace (LAPL1).
// Data wykonania projektu: 18.12.2021
// Semestr: 5
// Rok akademicki: 3
// Nazwisko autora: Cisowski
// Wersja: v1.0

namespace InterfejsUzytkownikaCs
{
	// Klasa reprezentująca przefiltrowany fragment bitmapy w jednym z wątków.
	public class WartoscZwracana
	{
		// 'Id' wątku aby na końcu wątek  znalazł odpowiedni fragment i zapisał tam swoje wyjście.
		public int IdWatku { get; set; }

		// Ilość indeksów tablicy bajtów, która jest filtrowana w tym fragmencie.
		public int IloscFiltrowanychIndeksow { get; set; }

		// Wynik filtrowania danego fragmentu bitmapy w postaci tablicy bajtów.
		public byte[] TablicaWyjsciowa { get; set; }
	}
}