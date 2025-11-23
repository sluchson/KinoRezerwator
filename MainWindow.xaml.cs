using System;
using System.Windows;

namespace KinoRezerwator
{
    public partial class MainWindow : Window
    {
        // Tworzymy instancję naszej klasy do bazy
        private readonly BazaDanych _baza = new BazaDanych();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnSzukaj_Click(object sender, RoutedEventArgs e)
        {
            string szukanaFraza = txtSzukaj.Text;

            try
            {
                // Wyłączamy przycisk, żeby użytkownik nie klikał 10 razy
                btnSzukaj.IsEnabled = false;
                btnSzukaj.Content = "Szukanie...";

                // 1. Wywołujemy naszą metodę z klasy BazaDanych
                // To wywoła funkcję SQL: fn_WyszukajSeansePorownawczo
                var wyniki = await _baza.WyszukajSeanse(szukanaFraza);

                // 2. Wrzucamy wyniki do listy na ekranie
                listaWynikow.ItemsSource = wyniki;

                if (wyniki.Count == 0)
                {
                    MessageBox.Show("Nie znaleziono seansów dla podanej frazy.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                // Jeśli coś pójdzie nie tak (np. złe hasło do bazy), wyświetl błąd
                MessageBox.Show($"Błąd bazy danych:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Włączamy przycisk z powrotem
                btnSzukaj.IsEnabled = true;
                btnSzukaj.Content = "Szukaj";
            }
        }

        private void listaWynikow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Sprawdzamy, co zostało kliknięte
            if (listaWynikow.SelectedItem is WynikSeansu wybranySeans)
            {
                // Tworzymy i otwieramy okno rezerwacji
                var okno = new RezerwacjaWindow(wybranySeans.IdSeansu, wybranySeans.TytulFilmu, wybranySeans.Kino);
                okno.ShowDialog(); // ShowDialog blokuje główne okno do czasu zamknięcia rezerwacji
            }
        }
    }
}