using System;
using System.Windows;
using System.Windows.Controls;

namespace KinoRezerwator
{
    public partial class AdminWindow : Window
    {
        private readonly BazaDanych _baza = new BazaDanych();

        public AdminWindow()
        {
            InitializeComponent();
            ZaladujListy();
        }

        private async void ZaladujListy()
        {
            try
            {
                cmbFilmy.ItemsSource = await _baza.PobierzFilmyLista();
                cmbKina.ItemsSource = await _baza.PobierzKinaLista();
            }
            catch (Exception ex) { MessageBox.Show("Błąd ładowania list: " + ex.Message); }
        }

        private async void cmbKina_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbKina.SelectedValue is int idKina)
            {
                cmbSale.ItemsSource = await _baza.PobierzSaleLista(idKina);
            }
        }

        private async void btnDodajSeans_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbFilmy.SelectedValue == null || cmbSale.SelectedValue == null)
                {
                    MessageBox.Show("Wybierz film, kino i salę!"); return;
                }

                int idFilmu = (int)cmbFilmy.SelectedValue;
                int idSali = (int)cmbSale.SelectedValue;

                if (!DateTime.TryParse(txtDataSeansu.Text, out DateTime data))
                {
                    MessageBox.Show("Błędny format daty!"); return;
                }

                string cenaTekst = txtCenaSeansu.Text.Replace(".", ",");
                if (!decimal.TryParse(cenaTekst, out decimal cena))
                {
                    MessageBox.Show("Błędna cena! Wpisz liczbę (np. 25,00)"); return;
                }

                await _baza.DodajSeans(idFilmu, idSali, data, cena);

                MessageBox.Show("Seans dodany pomyślnie!", "Sukces");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}");
            }
        }

        private async void btnDodajFilm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int czas = int.Parse(txtCzas.Text);
                await _baza.DodajFilm(txtTytul.Text, txtOpis.Text, czas, txtKategoria.Text);
                MessageBox.Show("Film dodany!");
                ZaladujListy();
            }
            catch (Exception ex) { MessageBox.Show("Błąd: " + ex.Message); }
        }

        private async void btnOdswiezRezerwacje_Click(object sender, RoutedEventArgs e)
        {
            try
            { 
                gridRezerwacje.ItemsSource = null;

                gridRezerwacje.ItemsSource = await _baza.PobierzRezerwacjeAdmin();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się pobrać rezerwacji.\nSzczegóły błędu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnUsunRezerwacje_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is RezerwacjaAdmin rez)
            {
                var decyzja = MessageBox.Show($"Czy na pewno usunąć rezerwację nr {rez.Id}?", "Potwierdź", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (decyzja == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _baza.UsunRezerwacje(rez.Id);
                        MessageBox.Show("Usunięto.");
                        btnOdswiezRezerwacje_Click(null, null);
                    }
                    catch (Exception ex) { MessageBox.Show("Błąd: " + ex.Message); }
                }
            }
        }
    }
}