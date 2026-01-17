using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace KinoRezerwator
{
    public class NowaSalaInfo
    {
        public string Nazwa { get; set; }
        public int Rzedy { get; set; }
        public int Miejsca { get; set; }

        public override string ToString()
        {
            return $"{Nazwa} ({Rzedy} rzędów x {Miejsca} miejsc)";
        }
    }

    public partial class AdminWindow : Window
    {
        private readonly BazaDanych _baza = new BazaDanych();
        private List<NowaSalaInfo> _tymczasoweSale = new List<NowaSalaInfo>();

        public AdminWindow()
        {
            InitializeComponent();
            this.Loaded += AdminWindow_Loaded;
        }

        #region Inicjalizacja Danych

        private void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ZaladujWszystkieDane();
        }

        private async void ZaladujWszystkieDane()
        {
            try
            {
                comboFilmy.ItemsSource = await _baza.PobierzFilmyLista();
                comboKina.ItemsSource = await _baza.PobierzKinaLista();
                comboFilmKategoria.ItemsSource = await _baza.PobierzKategorieLista();

                OdswiezFilmy();
                OdswiezSeanse();
                OdswiezKina();
                OdswiezStatystyki();
                btnOdswiezRezerwacje_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd ładowania danych startowych: " + ex.Message);
            }
        }

        #endregion

        #region Zakładka: Filmy

        private async void OdswiezFilmy()
        {
            try
            {
                var tabela = await _baza.PobierzFilmyTabela();
                gridFilmy.ItemsSource = tabela.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Błąd filmów: " + ex.Message); }
        }

        private async void btnZapiszFilm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string kategoriaTekst = comboFilmKategoria.Text;

                await _baza.DodajFilm(txtFilmTytul.Text, txtFilmOpis.Text, int.Parse(txtFilmCzas.Text), kategoriaTekst);
                MessageBox.Show("Film dodany!");
                OdswiezFilmy();
                comboFilmy.ItemsSource = await _baza.PobierzFilmyLista();
                comboFilmKategoria.ItemsSource = await _baza.PobierzKategorieLista();
            }
            catch (Exception ex) { MessageBox.Show("Błąd zapisu filmu: " + ex.Message); }
        }

        private async void btnUsunFilm_Click(object sender, RoutedEventArgs e)
        {
            if (gridFilmy.SelectedItem is DataRowView row)
            {
                if (MessageBox.Show("Usunięcie filmu usunie też jego seanse. Kontynuować?", "Potwierdź", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    await _baza.UsunFilm((int)row["id_filmu"]);
                    OdswiezFilmy();
                }
            }
            else MessageBox.Show("Zaznacz film.");
        }

        #endregion

        #region Zakładka: Seanse

        private async void OdswiezSeanse()
        {
            try
            {
                var tabela = await _baza.PobierzSeanseTabela();
                gridSeanse.ItemsSource = tabela.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Błąd seansów: " + ex.Message); }
        }

        private async void btnDodajSeans_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (comboFilmy.SelectedValue == null || comboSale.SelectedValue == null || datePicker.SelectedDate == null)
                {
                    MessageBox.Show("Wypełnij wszystkie pola!"); return;
                }

                int idFilmu = (int)comboFilmy.SelectedValue;
                int idSali = (int)comboSale.SelectedValue;
                decimal cena = decimal.Parse(txtCena.Text);
                TimeSpan godzina = TimeSpan.Parse(txtGodzina.Text);
                DateTime data = datePicker.SelectedDate.Value.Add(godzina);

                await _baza.DodajSeans(idFilmu, idSali, data, cena);
                MessageBox.Show("Seans dodany!");
                OdswiezSeanse();
            }
            catch (Exception ex) { MessageBox.Show("Błąd dodawania seansu: " + ex.Message); }
        }

        private async void btnUsunSeans_Click(object sender, RoutedEventArgs e)
        {
            if (gridSeanse.SelectedItem is DataRowView row)
            {
                await _baza.UsunSeans((int)row["id_seansu"]);
                OdswiezSeanse();
            }
            else MessageBox.Show("Zaznacz seans.");
        }

        private async void comboKina_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboKina.SelectedValue == null) return;
            try
            {
                if (comboKina.SelectedValue is int idKina)
                {
                    comboSale.ItemsSource = await _baza.PobierzSaleLista(idKina);
                }
            }
            catch { }
        }

        #endregion

        #region Zakładka: Kina

        private async void OdswiezKina()
        {
            try
            {
                var tabela = await _baza.PobierzKinaTabela();
                gridKina.ItemsSource = tabela.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Błąd kin: " + ex.Message); }
        }

        private void btnDodajSaleDoListy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string nazwa = txtNazwaSali.Text.Trim();

                if (!int.TryParse(txtRzedy.Text, out int rzedy) || rzedy <= 0)
                {
                    MessageBox.Show("Podaj poprawną liczbę rzędów."); return;
                }
                if (!int.TryParse(txtMiejsca.Text, out int miejsca) || miejsca <= 0)
                {
                    MessageBox.Show("Podaj poprawną liczbę miejsc."); return;
                }
                if (string.IsNullOrEmpty(nazwa))
                {
                    MessageBox.Show("Podaj nazwę sali."); return;
                }

                _tymczasoweSale.Add(new NowaSalaInfo
                {
                    Nazwa = nazwa,
                    Rzedy = rzedy,
                    Miejsca = miejsca
                });

                listaTymczasowychSal.ItemsSource = null;
                listaTymczasowychSal.ItemsSource = _tymczasoweSale;

                txtNazwaSali.Clear();
                txtRzedy.Text = "10";
                txtMiejsca.Text = "15";
            }
            catch (Exception ex) { MessageBox.Show("Błąd dodawania do listy: " + ex.Message); }
        }

        private async void btnZapiszKinoCalosc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtKinoNazwa.Text) || string.IsNullOrWhiteSpace(txtKinoAdres.Text))
                {
                    MessageBox.Show("Podaj nazwę i adres kina!");
                    return;
                }

                int noweIdKina = await _baza.DodajKino(txtKinoNazwa.Text, txtKinoAdres.Text);

                foreach (var sala in _tymczasoweSale)
                {
                    await _baza.DodajSale(noweIdKina, sala.Nazwa, sala.Rzedy, sala.Miejsca);
                }

                MessageBox.Show("Kino i sale zostały zapisane!");

                txtKinoNazwa.Clear();
                txtKinoAdres.Clear();
                _tymczasoweSale.Clear();
                listaTymczasowychSal.ItemsSource = null;

                OdswiezKina();
                comboKina.ItemsSource = await _baza.PobierzKinaLista();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zapisu kina: " + ex.Message);
            }
        }

        private async void btnUsunKino_Click(object sender, RoutedEventArgs e)
        {
            if (gridKina.SelectedItem is DataRowView row)
            {
                if (MessageBox.Show("Usunięcie kina usunie też jego sale. Kontynuować?", "Uwaga", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    await _baza.UsunKino((int)row["id_kina"]);
                    OdswiezKina();
                }
            }
            else MessageBox.Show("Zaznacz kino.");
        }

        #endregion

        #region Zakładka: Rezerwacje

        private async void btnOdswiezRezerwacje_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                gridRezerwacje.ItemsSource = null;
                gridRezerwacje.ItemsSource = await _baza.PobierzRezerwacjeAdmin();
            }
            catch { }
        }

        private async void btnAnuluj_Click(object sender, RoutedEventArgs e)
        {
            if (gridRezerwacje.SelectedItem is RezerwacjaAdmin rez)
            {
                await _baza.UsunRezerwacje(rez.Id);
                btnOdswiezRezerwacje_Click(null, null);
            }
            else MessageBox.Show("Zaznacz rezerwację.");
        }

        #endregion

        #region Zakładka: Statystyki

        private async void OdswiezStatystyki()
        {
            try
            {
                var globalne = await _baza.PobierzStatystykiGlobalne();
                txtTotalBilety.Text = globalne.bilety.ToString();
                txtTotalPrzychod.Text = $"{globalne.kasa:N2} zł";

                var filmyDt = await _baza.PobierzRankingFilmow();
                gridTopFilmy.ItemsSource = filmyDt.DefaultView;

                var kategorieDt = await _baza.PobierzRankingKategorii();
                gridTopKategorie.ItemsSource = kategorieDt.DefaultView;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Błąd statystyk: " + ex.Message);
            }
        }

        private void btnOdswiezStatystyki_Click(object sender, RoutedEventArgs e)
        {
            OdswiezStatystyki();
        }

        #endregion
    }
}