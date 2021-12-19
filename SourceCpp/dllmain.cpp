// Temat: Algorytm na bitmapie - filtrowanie Laplace (LAPL1).
// Krótki opis: Algorytm filtrujący przekazaną z dysku (za pomocą graficznego UI) bitmapę za pomocą filtru Laplace (LAPL1).
// Data wykonania projektu: 18.12.2021
// Semestr: 5
// Rok akademicki: 3
// Nazwisko autora: Cisowski
// Wersja: v1.0

#include "pch.h"
#include <algorithm>
using namespace std;

// Funkcja wywołana automatycznie w momencie pierwszego wejścia programu do DLL.
BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

// Funkcja tworząca, inicjalizująca i zwracająca tablicę (long*) która przechowuje wartości maski którą algorytm filtruję bitmapę - LAPL1.
long* InicjalizujMaski()
{
	long* maski = new long[9];

	// http://www.algorytm.org/przetwarzanie-obrazow/filtrowanie-obrazow.html - filtr LAPL1

	// Inicjalizacja maski
	//  0 -1  0
	// -1  4 -1
	//  0 -1  0
	for (int i = 0; i < 9; i++)
	{
		if (i % 2 == 0)
		{
			maski[i] = 0;
		}
		else
		{
			maski[i] = -1;
		}

		if (i == 4)
		{
			maski[i] = 4;
		}
	}

	return maski;
}

// Funkcja sumująca maski które są przechowane w podanej poprzez arguent tablicy. Podana musi być również ich ilość.
int SumujMaski(long* maski, int iloscMasek)
{
	int suma = 0;

	for (int i = 0; i < iloscMasek; i++)
	{
		suma += maski[i];
	}

	return suma;
}

// Funkcja obliczająca wartość piksela na podstawie maski.
// Parametry: fragment - tablica 3x3 która reprezentuje fragment filtrowanej bitmapy (będziemy obliczać wartość piksela w centrum)
//			  maski - tablica 3x3 maski filtrującej
// Zwraca warto z przedziału od 0 do 255 włącznie, i jest nią nowa wyliczona wartość piksela w centrum tablicy 3x3.
unsigned char ObliczNowaWartoscPiksela(unsigned char* fragment, long* maski)
{
	// Inicjalizujemy wartość piksela
	int wartosc = 0;
	int sumaMasek = SumujMaski(maski, 9);

	// Zgodnie ze wzorem z algorytmu, na początku dodajemy do wartości skladniki wyliczone na podstawie wartosci maski i wartosci piksela)
	for (int j = 0; j < 3; j++)
	{
		for (int i = 0; i < 3; i++)
		{
			int skladnik = fragment[i + j * 3] * maski[i + j * 3];

			wartosc += skladnik;
		}
	}

	// Na wypadek gdyby wartość wyszła poza granice (0-255), ustawiamy ją na graniczną wartość.
	wartosc = clamp<int>(wartosc, 0, 255);

	// Zgodnie z algorytmem, dzielimy otrzymaną wartość przez sumę masek.
	if (sumaMasek != 0)
	{
		wartosc = (wartosc / (double)sumaMasek);
	}

	// Zwracamy wartość piksela
	return (unsigned char)wartosc;
}

// Główna funkcja nakładająca filtr LAPL1.
// Przyjmuje jako parametry: wskaznik na wejsciowa tablice bajtów (przekazaną bitmapę), wskaznik na tablice wyjściową (do której zapiszemy przefiltrowany fragment)
//							 dlugosc bitamy, szerokosc bitmapy, indeks startowy filtrowania fragmentu, ilosc indeksow jakie bedziemy filtrowac.
// Funkcja przefiltruje podany fragment i zapisze go do wyjściowej tablicy.
extern "C" __declspec(dllexport) void __stdcall NalozFiltrCpp(unsigned char* wskaznikNaWejsciowaTablice, unsigned char* wskaznikNaWyjsciowaTablice, int dlugoscBitmapy, int szerokoscBitmapy, int indeksStartowy, int ileIndeksowFiltrowac)
{
	// Na początku inicjalizujemy maski wartościami z filtru Laplace LAPL1
	long* maski = InicjalizujMaski();

	// Iterujemy się po każdym indeksie fragmentu który musimy przefiltrować (w każdej iteracji operujemy na 3 indeksach R,G,B)
	for (int i = indeksStartowy; i < indeksStartowy + ileIndeksowFiltrowac; i += 3)
	{
		// Pomijamy indeksy bitmapy które leżą na krawędzi - ich nie filtrujemy zgodnie z algorytmem.
		if ((i < szerokoscBitmapy) || (i % szerokoscBitmapy == 0) || (i >= dlugoscBitmapy - szerokoscBitmapy) || ((i + 2 + 1) % szerokoscBitmapy == 0))
		{
			continue;
		}

		// Inicjalizujemy tablicę od wartości tablicy 3x3 odpowiednio R, G i B.
		unsigned char* r = new unsigned char[9];
		unsigned char* g = new unsigned char[9];
		unsigned char* b = new unsigned char[9];

		// Sczytujemy wartości z opbszaru 3x3 wokół obecnego piksela i zapisujemy je do tablic r,g,b.
		for (int y = 0; y < 3; y++)
		{
			for (int x = 0; x < 3; x++)
			{
				int indeksPiksela = i + (szerokoscBitmapy * (y - 1) + (x - 1) * 3);

				int indeksRGB = x * 3 + y;

				r[indeksRGB] = wskaznikNaWejsciowaTablice[indeksPiksela];

				indeksPiksela++;

				g[indeksRGB] = wskaznikNaWejsciowaTablice[indeksPiksela];

				indeksPiksela++;

				b[indeksRGB] = wskaznikNaWejsciowaTablice[indeksPiksela];
			}
		}

		// Zapisujemy wartości przefiltrowanych pikseli (dla R,G,B) do wyjściowej tablicy.
		int indeksPikselaWyjscie = i - indeksStartowy;
		wskaznikNaWyjsciowaTablice[indeksPikselaWyjscie] = ObliczNowaWartoscPiksela(r, maski);
		indeksPikselaWyjscie++;
		wskaznikNaWyjsciowaTablice[indeksPikselaWyjscie] = ObliczNowaWartoscPiksela(g, maski);
		indeksPikselaWyjscie++;
		wskaznikNaWyjsciowaTablice[indeksPikselaWyjscie] = ObliczNowaWartoscPiksela(b, maski);

		// Usuwamy maski, by zapobiec wyciekom pamięci.
		delete[] r;
		delete[] g;
		delete[] b;
	}

	// Usuwamy maski, by zapobiec wyciekom pamięci.
	delete[] maski;
}