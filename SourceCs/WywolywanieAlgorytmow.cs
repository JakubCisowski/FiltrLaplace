using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SourceCs
{
    public class WywolywanieAlgorytmow
    {
        public static async Task<byte[]> WywolajAlgorytmCs(byte[] bitmapaTablicaBajtow, int iloscWatkow)
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
                        Task<IntPtr> taskWTymWatku = Task.Run(() => Algorytm.NalozFiltrCs(wskaznik, bitmapaTablicaBajtow.Length, indeksStartowy, ileIndeksowFiltrowac));
                        listaWatkow.Add(taskWTymWatku);
                    }
                }
            }

            await Task.WhenAll(listaWatkow);

            // Póki co losowy wynik, póżniej trzeba będzie połączyć rezultaty z wątków.
            return new byte[] { 0, 1, 2, 3, 4, 5 };
        }

        // Dynamicznie - import dll z algorytmem w asm.
        [DllImport(@"C:\Programowanie\Studia\JA\FiltrLaplace\x64\Debug\ProjektJA.Asm.dll")]
        public static extern IntPtr NalozFiltrAsm(IntPtr bitmapaTablicaBajtow, int dlugoscBitmapy, int indeksStartowy, int ileIndeksowFiltrowac);

        public static async Task<byte[]> WywolajAlgorytmAsm(byte[] bitmapaTablicaBajtow, int iloscWatkow)
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
                        Task<IntPtr> taskWTymWatku = Task.Run(() => NalozFiltrAsm(wskaznik, bitmapaTablicaBajtow.Length, indeksStartowy, ileIndeksowFiltrowac));
                        listaWatkow.Add(taskWTymWatku);
                    }
                }
            }

            await Task.WhenAll(listaWatkow);

            // Póki co losowy wynik, póżniej trzeba będzie połączyć rezultaty z wątków.
            return new byte[] { 0, 1, 2, 3, 4, 5 };
        }
    }
}