using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq; 
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; 
using System.Windows.Media;

namespace KinoRezerwator
{
    public partial class RezerwacjaWindow : Window
    {
        private readonly BazaDanych _baza = new BazaDanych();
        private readonly int _idSeansu;

        private List<int> _wybraneMiejsca = new List<int>();

        public RezerwacjaWindow(int idSeansu, string tytul, string kino)
        {
            InitializeComponent();
            _idSeansu = idSeansu;
            txtTytulFilmu.Text = $"{tytul}\n{kino}";

            ZaladujMape();
        }

        private async void ZaladujMape()
        {
            try
            {
                var miejsca = await _baza.PobierzMapeSali(_idSeansu);

                if (miejsca.Count == 0)
                {
                    MessageBox.Show("Brak zdefiniowanych miejsc dla tej sali.");
                    return;
                }

                gridMiejsca.Children.Clear();

                int maxNumer = miejsca.Max(m => m.Numer);
                gridMiejsca.Columns = maxNumer;

                foreach (var miejsce in miejsca)
                {
                    var btn = new Button();
                    btn.Content = $"{miejsce.Rzad}-{miejsce.Numer}"; 
                    btn.Margin = new Thickness(2);
                    btn.Width = 40;
                    btn.Height = 40;
                    btn.Tag = miejsce.IdMiejsca; 

                    if (miejsce.Status == "zajete")
                    {
                        btn.Background = Brushes.Red;
                        btn.IsHitTestVisible = false; 
                        btn.Opacity = 0.6; 
                    }
                    else
                    {
                        btn.Background = Brushes.Green;
                        btn.Click += Miejsce_Click; 
                    }

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
                _wybraneMiejsca.Remove(idMiejsca);
                btn.Background = Brushes.Green;
            }
            else
            {
                _wybraneMiejsca.Add(idMiejsca);
                btn.Background = Brushes.Blue;
            }
        }

        private async void btnRezerwuj_Click(object sender, RoutedEventArgs e)
        {
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
                btnRezerwuj.IsEnabled = false;
                btnRezerwuj.Content = "Przetwarzanie...";

                await _baza.ZrobRezerwacje(_idSeansu, _wybraneMiejsca, imie, email);

                MessageBox.Show("Rezerwacja zakończona sukcesem!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (PostgresException ex)
            {
                MessageBox.Show($"Błąd rezerwacji: {ex.MessageText}", "Błąd Bazy Danych", MessageBoxButton.OK, MessageBoxImage.Error);

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