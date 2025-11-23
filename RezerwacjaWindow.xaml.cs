using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq; // Ważne dla funkcji Max()
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // Dla UniformGrid
using System.Windows.Media;

namespace KinoRezerwator
{
    public partial class RezerwacjaWindow : Window
    {
        private readonly BazaDanych _baza = new BazaDanych();
        private readonly int _idSeansu;

        // Lista ID miejsc, które użytkownik kliknął
        private List<int> _wybraneMiejsca = new List<int>();

        // Konstruktor przyjmuje ID seansu i tytuł, żeby wiedzieć co wyświetlić
        public RezerwacjaWindow(int idSeansu, string tytul, string kino)
        {
            InitializeComponent();
            _idSeansu = idSeansu;
            txtTytulFilmu.Text = $"{tytul}\n{kino}";

            // Po otwarciu okna, od razu ładujemy mapę
            ZaladujMape();
        }

        private async void ZaladujMape()
        {
            try
            {
                // 1. Pobieramy dane z bazy
                var miejsca = await _baza.PobierzMapeSali(_idSeansu);

                if (miejsca.Count == 0)
                {
                    MessageBox.Show("Brak zdefiniowanych miejsc dla tej sali.");
                    return;
                }

                // 2. Czyścimy siatkę (na wszelki wypadek)
                gridMiejsca.Children.Clear();

                // 3. Ustawiamy liczbę kolumn dynamicznie
                // (znajdujemy największy numer miejsca w rzędzie)
                int maxNumer = miejsca.Max(m => m.Numer);
                gridMiejsca.Columns = maxNumer;

                foreach (var miejsce in miejsca)
                {
                    // Tworzymy przycisk dla każdego miejsca
                    var btn = new Button();
                    btn.Content = $"{miejsce.Rzad}-{miejsce.Numer}"; // Np. "1-5"
                    btn.Margin = new Thickness(2);
                    btn.Width = 40;
                    btn.Height = 40;
                    btn.Tag = miejsce.IdMiejsca; // Zapamiętujemy ID w tagu

                    // Kolorowanie
                    if (miejsce.Status == "zajete")
                    {
                        btn.Background = Brushes.Red;
                        // ZAMIAST btn.IsEnabled = false; (co robi szary kolor)
                        // ROBIMY TO:
                        btn.IsHitTestVisible = false; // Przycisk jest czerwony, ale myszka go "nie widzi" (nie da się kliknąć)
                        btn.Opacity = 0.6; // Opcjonalnie: lekko przezroczysty, żeby było widać, że to "tło"
                    }
                    else
                    {
                        btn.Background = Brushes.Green;
                        btn.Click += Miejsce_Click; // Podpinamy zdarzenie kliknięcia tylko dla wolnych
                    }

                    // 4. Dodajemy przycisk BEZPOŚREDNIO do siatki
                    gridMiejsca.Children.Add(btn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania mapy: {ex.Message}");
            }
        }   

        private void Miejsce_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int idMiejsca = (int)btn.Tag;

            if (_wybraneMiejsca.Contains(idMiejsca))
            {
                // Odznaczamy
                _wybraneMiejsca.Remove(idMiejsca);
                btn.Background = Brushes.Green;
            }
            else
            {
                // Zaznaczamy
                _wybraneMiejsca.Add(idMiejsca);
                btn.Background = Brushes.Blue;
            }
        }

        private async void btnRezerwuj_Click(object sender, RoutedEventArgs e)
        {
            // 1. Walidacja formularza (w C#)
            if (_wybraneMiejsca.Count == 0)
            {
                MessageBox.Show("Nie wybrano żadnych miejsc!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string imie = txtImie.Text.Trim();
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(imie) || string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Proszę podać Imię i E-mail.", "Brak danych", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Wyłączamy przycisk, żeby nie kliknąć dwa razy
                btnRezerwuj.IsEnabled = false;
                btnRezerwuj.Content = "Przetwarzanie...";

                // 2. Wywołanie procedury w bazie
                await _baza.ZrobRezerwacje(_idSeansu, _wybraneMiejsca, imie, email);

                // 3. Sukces!
                MessageBox.Show("Rezerwacja zakończona sukcesem!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

                // Zamykamy okno rezerwacji
                this.Close();
            }
            catch (PostgresException ex)
            {
                // TUTAJ ŁAPIEMY BŁĘDY Z PROCEDURY SQL (np. "RAISE EXCEPTION")
                // Jeśli baza zwróci błąd "Miejsce zajęte", to tutaj go wyświetlimy.
                MessageBox.Show($"Błąd rezerwacji: {ex.MessageText}", "Błąd Bazy Danych", MessageBoxButton.OK, MessageBoxImage.Error);

                // Odświeżamy mapę, żeby pokazać, co się zmieniło
                ZaladujMape();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił nieoczekiwany błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnRezerwuj.IsEnabled = true;
                btnRezerwuj.Content = "POTWIERDZAM REZERWACJĘ";
            }
        
        }
    }
}