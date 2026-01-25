using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq; 
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; 
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace KinoRezerwator
{
    public partial class RezerwacjaWindow : Window
    {
        private readonly BazaDanych _baza = new BazaDanych();
        private readonly int _idSeansu;

        private List<int> _wybraneMiejsca = new List<int>();

        public RezerwacjaWindow(int idSeansu, string tytul, string kino, string opis)
        {
            InitializeComponent();
            _idSeansu = idSeansu;

            txtTytulFilmu.Text = $"{tytul} ({kino})";
            txtOpis.Text = opis; 

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

                    if (miejsce.Status == "Zajęte")
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
            string imieNazwisko = txtImie.Text.Trim();
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(imieNazwisko))
            {
                MessageBox.Show("Proszę podać Imię i Nazwisko.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string imieWzorzec = @"^[A-ZĄĆĘŁŃÓŚŹŻ][a-ząćęłńóśźż]+\s+[A-ZĄĆĘŁŃÓŚŹŻ][a-ząćęłńóśźż]+$";
            if (!Regex.IsMatch(imieNazwisko, imieWzorzec))
            {
                MessageBox.Show("Podaj poprawne Imię i Nazwisko (np. 'Jan Kowalski').\nOba człony muszą zaczynać się z wielkiej litery.",
                                "Błąd formatu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Proszę podać adres E-mail.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string emailWzorzec = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

            if (!Regex.IsMatch(email, emailWzorzec))
            {
                MessageBox.Show("Podany adres E-mail jest nieprawidłowy.\nUpewnij się, że nie zawiera polskich znaków (np. ą, ź) ani spacji.",
                                "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_wybraneMiejsca.Count == 0)
            {
                MessageBox.Show("Proszę zaznaczyć przynajmniej jedno miejsce na mapie.", "Brak miejsc", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                btnRezerwuj.IsEnabled = false;

                await _baza.ZrobRezerwacje(_idSeansu, _wybraneMiejsca, imieNazwisko, email);

                MessageBox.Show("Rezerwacja została pomyślnie utworzona!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd rezerwacji: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnRezerwuj.IsEnabled = true;
            }
        }
    }
}