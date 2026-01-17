using System;
using System.Windows;

namespace KinoRezerwator
{
    public partial class LoginWindow : Window
    {
        #region Pola i Inicjalizacja

        private readonly BazaDanych _baza = new BazaDanych();

        public LoginWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Logika Logowania

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
                var (sukces, komunikat) = await _baza.ZalogujAdmina(login, haslo);

                if (sukces)
                {
                    MessageBox.Show(komunikat, "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                    var adminPanel = new AdminWindow();
                    adminPanel.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show(komunikat, "Błąd logowania", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd krytyczny połączenia: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnZaloguj.IsEnabled = true;
            }
        }

        #endregion
    }
}