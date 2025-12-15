using System;
using System.Windows;

namespace KinoRezerwator
{
    public partial class MainWindow : Window
    {
        private readonly BazaDanych _baza = new BazaDanych();

        public MainWindow()
        {
            InitializeComponent();
            WyszukajFilmy("");
        }

        private void btnSzukaj_Click(object sender, RoutedEventArgs e)
        {
            WyszukajFilmy(txtSzukaj.Text);
        }

        private void btnAdmin_Click(object sender, RoutedEventArgs e)
        {
            var oknoLogowania = new LoginWindow();
            oknoLogowania.ShowDialog();
        }

        private void listaWynikow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (listaWynikow.SelectedItem is WynikSeansu wybranySeans)
            {
                var okno = new RezerwacjaWindow(wybranySeans.IdSeansu, wybranySeans.TytulFilmu, wybranySeans.Kino);
                okno.ShowDialog();
            }
        }

        private async void WyszukajFilmy(string szukanaFraza)
        {
            try
            {
                btnSzukaj.IsEnabled = false;
                btnSzukaj.Content = "Szukanie...";

                var wyniki = await _baza.WyszukajSeanse(szukanaFraza);

                listaWynikow.ItemsSource = wyniki;

                if (wyniki.Count == 0)
                {
                    MessageBox.Show("Nie znaleziono seansów.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd bazy danych:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSzukaj.IsEnabled = true;
                btnSzukaj.Content = "Szukaj";
            }
        }
    }
}