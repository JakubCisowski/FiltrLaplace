using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SourceCs
{
    public class WywolywanieAlgorytmow
    {
        [DllImport(@"C:\Programowanie\Studia\JA\FiltrLaplace\x64\Debug\SourceCpp.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int NalozFiltrCpp(IntPtr bitmapaTablicaBajtow, int dlugoscBitmapy, int indeksStartowy, int ileIndeksowFiltrowac);

        public static async Task<int> WywolajAlgorytmCs(byte[] bitmapaTablicaBajtow, int iloscWatkow)
        {
            int indeks = 0;
            // Kalkulujemy ilosc bajtow na jeden watek.
            int iloscBajtowNaJedenWatek = bitmapaTablicaBajtow.Length / iloscWatkow;
            // Tworzymy liste wszystkich watkow.
            List<Task> listaWatkow = new List<Task>();

            unsafe
            {
                for (int i = 0; i < iloscWatkow; i++)
                {
                    // Kalkulujemy indeks startowy.
                    int indeksStartowy = indeks;
                    int ileIndeksowFiltrowac = 0;
                    indeks += iloscBajtowNaJedenWatek;

                    // Kalklujemy ile indeksów należy filtrować.
                    if (i != iloscWatkow - 1)
                    {
                        ileIndeksowFiltrowac = iloscBajtowNaJedenWatek;
                    }
                    else
                    {
                        ileIndeksowFiltrowac = bitmapaTablicaBajtow.Length - indeksStartowy;
                    }

                    // Wywolanie filtra za pomoca wskaznika na tablice bajtów czyli bitmapę.
                    fixed (byte* wskaznikNaTabliceBajtow = &bitmapaTablicaBajtow[0])
                    {
                        IntPtr wskaznik = new IntPtr(wskaznikNaTabliceBajtow);
                        Task<int> taskWTymWatku = Task.Run(() => NalozFiltrCpp(wskaznik, bitmapaTablicaBajtow.Length, indeksStartowy, ileIndeksowFiltrowac));
                        listaWatkow.Add(taskWTymWatku);
                    }
                }
            }

            await Task.WhenAll(listaWatkow);

            int trzeciBajtWTablicy = ((Task<int>)listaWatkow[0]).Result;

            // Tymczasowo, póżniej trzeba będzie połączyć rezultaty z wątków.
            return trzeciBajtWTablicy;
        }

        // Dynamicznie - import dll z algorytmem w asm.
        [DllImport(@"C:\Programowanie\Studia\JA\FiltrLaplace\x64\Debug\DllAsm.dll")]
        public static extern int NalozFiltrAsm(IntPtr bitmapaTablicaBajtow, int dlugoscBitmapy, int indeksStartowy, int ileIndeksowFiltrowac);

        public static async Task<int> WywolajAlgorytmAsm(byte[] bitmapaTablicaBajtow, int iloscWatkow)
        {
            int indeks = 0;
            // Kalkulujemy ilosc bajtow na jeden watek.
            int iloscBajtowNaJedenWatek = bitmapaTablicaBajtow.Length / iloscWatkow;
            // Tworzymy liste wszystkich watkow.
            List<Task> listaWatkow = new List<Task>();

            unsafe
            {
                for (int i = 0; i < iloscWatkow; i++)
                {
                    // Kalkulujemy indeks startowy.
                    int indeksStartowy = indeks;
                    int ileIndeksowFiltrowac = 0;
                    indeks += iloscBajtowNaJedenWatek;

                    // Kalklujemy ile indeksów należy filtrować.
                    if (i != iloscWatkow - 1)
                    {
                        ileIndeksowFiltrowac = iloscBajtowNaJedenWatek;
                    }
                    else
                    {
                        ileIndeksowFiltrowac = bitmapaTablicaBajtow.Length - indeksStartowy;
                    }

                    // Wywolanie filtra za pomoca wskaznika na tablice bajtów czyli bitmapę.
                    fixed (byte* wskaznikNaTabliceBajtow = &bitmapaTablicaBajtow[0])
                    {
                        IntPtr wskaznik = new IntPtr(wskaznikNaTabliceBajtow);
                        Task<int> taskWTymWatku = Task.Run(() => NalozFiltrAsm(wskaznik, bitmapaTablicaBajtow.Length, indeksStartowy, ileIndeksowFiltrowac));
                        listaWatkow.Add(taskWTymWatku);
                    }
                }
            }

            await Task.WhenAll(listaWatkow);

            int trzeciBajtWTablicy = ((Task<int>)listaWatkow[0]).Result;

            // Tymczasowo, póżniej trzeba będzie połączyć rezultaty z wątków.
            return trzeciBajtWTablicy;
        }
    }
}