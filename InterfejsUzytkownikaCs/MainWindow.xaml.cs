using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Windows;

namespace InterfejsUzytkownikaCs
{
    public partial class MainWindow : Window
    {
        private int iloscWatkow;
        private bool czyAsembler;

        private string sciezkaDoPliku;

        private byte[] bitmapaTablicaBajtow;

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
            var headerBitmapy = bitmapaTablicaBajtow.Take(54);
            var bitmapaBezHeadera = new byte[bitmapaTablicaBajtow.Length - 54];

            // Przepisujemy bajty by otrzymać bitmapę bez headera - jego nie potrzeba filtrować.
            for (int i = 54; i < bitmapaTablicaBajtow.Length; i++)
            {
                bitmapaBezHeadera[i - 54] = bitmapaTablicaBajtow[i];
            }

            if (czyAsembler)
            {
                int wynik = 0;
                wynik = await SourceCs.WywolywanieAlgorytmow.WywolajAlgorytmAsm(bitmapaBezHeadera, iloscWatkow);
                MessageBox.Show($"{wynik.ToString()}");
            }
            else
            {
                int wynik = 0;
                await SourceCs.WywolywanieAlgorytmow.WywolajAlgorytmCs(bitmapaBezHeadera, iloscWatkow);
                MessageBox.Show($"{wynik.ToString()}");
            }
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

                BitmapyPanel.Children.Clear();
                SciezkaDoPlikuBox.Text = nazwaPliku;
                sciezkaDoPliku = nazwaPliku;
                var bajtyBitmapy = File.ReadAllBytes(sciezkaDoPliku);
                bitmapaTablicaBajtow = bajtyBitmapy;
                FiltrujBitmapePrzycisk.IsEnabled = true;
            }
        }
    }
}