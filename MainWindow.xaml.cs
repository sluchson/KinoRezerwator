using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KinoRezerwator
{
    public partial class MainWindow : Window
    {
        #region Pola i Konstruktor

        private readonly BazaDanych _baza = new BazaDanych();
        private GridViewColumnHeader _ostatniaKliknietaKolumna = null;
        private bool _czyRosnaco = true;

        public MainWindow()
        {
            InitializeComponent();
            StartProgramu();
        }

        #endregion

        #region Inicjalizacja

        private async void StartProgramu()
        {
            try
            {
                var kategorie = await _baza.PobierzKategorieLista();
                kategorie.Insert(0, new ProstyObiekt { Id = -1, Nazwa = "--- Wszystkie ---" });

                comboKategorie.ItemsSource = kategorie;
                comboKategorie.SelectedIndex = 0;

                WyszukajFilmy("");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd startu: " + ex.Message);
            }
        }

        #endregion

        #region Logika Wyszukiwania

        private void btnSzukaj_Click(object sender, RoutedEventArgs e)
        {
            WyszukajFilmy(txtSzukaj.Text);
        }

        private async void WyszukajFilmy(string szukanaFraza)
        {
            try
            {
                btnSzukaj.IsEnabled = false;
                btnSzukaj.Content = "Szukanie...";

                int? idKategorii = null;
                if (comboKategorie.SelectedValue is int wybraneId && wybraneId != -1)
                {
                    idKategorii = wybraneId;
                }

                DateTime? wybranaData = datePicker.SelectedDate;

                var wyniki = await _baza.WyszukajSeanse(szukanaFraza, idKategorii, wybranaData);

                listaWynikow.ItemsSource = wyniki;

                if (wyniki.Count == 0)
                {
                    MessageBox.Show("Brak seansów dla wybranych kryteriów.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wyszukiwania:\n{ex.Message}");
            }
            finally
            {
                btnSzukaj.IsEnabled = true;
                btnSzukaj.Content = "Szukaj";
            }
        }

        #endregion

        #region Logika Sortowania Listy

        private void listaWynikow_Click(object sender, RoutedEventArgs e)
        {
            var naglowek = e.OriginalSource as GridViewColumnHeader;
            if (naglowek == null || naglowek.Column == null) return;

            var lista = listaWynikow.ItemsSource as List<WynikSeansu>;
            if (lista == null) return;

            string nazwaKolumny = naglowek.Content.ToString();

            if (_ostatniaKliknietaKolumna == naglowek)
            {
                _czyRosnaco = !_czyRosnaco;
            }
            else
            {
                _czyRosnaco = true;
                _ostatniaKliknietaKolumna = naglowek;
            }

            lista = SortujListe(lista, nazwaKolumny, _czyRosnaco);

            listaWynikow.ItemsSource = null;
            listaWynikow.ItemsSource = lista;
        }

        private List<WynikSeansu> SortujListe(List<WynikSeansu> lista, string kolumna, bool rosnaco)
        {
            switch (kolumna)
            {
                case "Film":
                    return rosnaco ? lista.OrderBy(x => x.TytulFilmu).ToList()
                                   : lista.OrderByDescending(x => x.TytulFilmu).ToList();

                case "Kategoria":
                    return rosnaco ? lista.OrderBy(x => x.Kategoria).ToList()
                                   : lista.OrderByDescending(x => x.Kategoria).ToList();

                case "Data":
                case "Godzina":
                    return rosnaco ? lista.OrderBy(x => x.DataGodzina).ToList()
                                   : lista.OrderByDescending(x => x.DataGodzina).ToList();

                case "Miejsce":
                    return rosnaco ? lista.OrderBy(x => x.Miejsce).ToList()
                                   : lista.OrderByDescending(x => x.Miejsce).ToList();

                case "Cena":
                    return rosnaco ? lista.OrderBy(x => x.Cena).ToList()
                                   : lista.OrderByDescending(x => x.Cena).ToList();

                default:
                    return lista;
            }
        }

        #endregion

        #region Nawigacja i Akcje

        private void btnAdmin_Click(object sender, RoutedEventArgs e)
        {
            var oknoLogowania = new LoginWindow();
            oknoLogowania.ShowDialog();
        }

        private void listaWynikow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listaWynikow.SelectedItem is WynikSeansu wybranySeans)
            {
                var okno = new RezerwacjaWindow(
                    wybranySeans.IdSeansu,
                    wybranySeans.TytulFilmu,
                    wybranySeans.Kino,
                    wybranySeans.Opis
                );
                okno.ShowDialog();
            }
        }

        #endregion
    }
}