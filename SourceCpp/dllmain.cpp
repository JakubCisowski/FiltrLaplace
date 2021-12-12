// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <algorithm>
using namespace std;

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
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

long* InicjalizujMaski()
{
	long* maski = new long[9];

	// http://www.algorytm.org/przetwarzanie-obrazow/filtrowanie-obrazow.html - filtr LAPL1

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

int SumujMaski(long* maski, int iloscMasek)
{
	int suma = 0;

	for (int i = 0; i < iloscMasek; i++)
	{
		suma += maski[i];
	}

	return suma;
}

unsigned char ObliczNowaWartoscPiksela(unsigned char* fragment, long* maski)
{
	int wartosc = 0;
	int sumaMasek = SumujMaski(maski, 9);

	for (int j = 0; j < 3; j++)
	{
		for (int i = 0; i < 3; i++)
		{
			int skladnik = fragment[i + j * 3] * maski[i + j * 3];

			wartosc += skladnik;
		}
	}

	wartosc = clamp<int>(wartosc, 0, 255);

	if (sumaMasek != 0)
	{
		wartosc = (wartosc / (double)sumaMasek);
	}

	return (unsigned char)wartosc;
}

extern "C" __declspec(dllexport) void __stdcall NalozFiltrCpp(unsigned char* wskaznikNaWejsciowaTablice, unsigned char* wskaznikNaWyjsciowaTablice, int dlugoscBitmapy, int szerokoscBitmapy, int indeksStartowy, int ileIndeksowFiltrowac)
{
	long* maski = InicjalizujMaski();

	for (int i = indeksStartowy; i < indeksStartowy + ileIndeksowFiltrowac; i += 3)
	{
		if (i < szerokoscBitmapy)
		{
			continue;
		}
		if (i % szerokoscBitmapy == 0)
		{
			continue;
		}
		if (i >= dlugoscBitmapy - szerokoscBitmapy)
		{
			continue;
		}
		if ((i + 2 + 1) % szerokoscBitmapy == 0)
		{
			continue;
		}

		unsigned char* r = new unsigned char[9];
		unsigned char* g = new unsigned char[9];
		unsigned char* b = new unsigned char[9];

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

		int indeksPikselaWyjscie = i - indeksStartowy;

		wskaznikNaWyjsciowaTablice[indeksPikselaWyjscie] = ObliczNowaWartoscPiksela(r, maski);

		indeksPikselaWyjscie++;

		wskaznikNaWyjsciowaTablice[indeksPikselaWyjscie] = ObliczNowaWartoscPiksela(g, maski);

		indeksPikselaWyjscie++;

		wskaznikNaWyjsciowaTablice[indeksPikselaWyjscie] = ObliczNowaWartoscPiksela(b, maski);

		delete[] r;
		delete[] g;
		delete[] b;
	}

	delete[] maski;
}

