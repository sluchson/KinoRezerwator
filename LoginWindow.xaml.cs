using System;
using System.Windows;

namespace KinoRezerwator
{
    public partial class LoginWindow : Window
    {
        private readonly BazaDanych _baza = new BazaDanych();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void btnZaloguj_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text;
            string haslo = txtHaslo.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(haslo))
            {
                MessageBox.Show("Podaj login i hasło.");
                return;
            }

            try
            {
                btnZaloguj.IsEnabled = false;

                bool czyZalogowano = await _baza.ZalogujAdmina(login, haslo);

                if (czyZalogowano)
                {
                    MessageBox.Show("Zalogowano pomyślnie!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                    var adminPanel = new AdminWindow();
                    adminPanel.Show();

                    this.Close();
                }
                else
                {
                    MessageBox.Show("Błędny login lub hasło.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd połączenia: " + ex.Message);
            }
            finally
            {
                btnZaloguj.IsEnabled = true;
            }
        }
    }
}