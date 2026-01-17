using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace KinoRezerwator
{
    #region Modele Danych

    public class Miejsce
    {
        public int IdMiejsca { get; set; }
        public int Rzad { get; set; }
        public int Numer { get; set; }
        public string Status { get; set; }
    }

    public class WynikSeansu
    {
        public int IdSeansu { get; set; }
        public string TytulFilmu { get; set; }
        public string Opis { get; set; }
        public string Kategoria { get; set; }
        public string Kino { get; set; }
        public string Sala { get; set; }
        public DateTime DataGodzina { get; set; }
        public decimal Cena { get; set; }
        public string Miejsce => $"{Kino} ({Sala})";
    }

    public class ProstyObiekt
    {
        public int Id { get; set; }
        public string Nazwa { get; set; }
    }

    public class RezerwacjaAdmin
    {
        public int Id { get; set; }
        public string Film { get; set; }
        public string Kino { get; set; }
        public DateTime Data { get; set; }
        public string Klient { get; set; }
        public long IloscMiejsc { get; set; }
    }

    #endregion

    public class BazaDanych
    {
        private const string ConnectionString = "Host=localhost;Username=postgres;Password=admin;Database=KinoRezerwator";

        #region Metody Pomocnicze (Helpers)

        private async Task<List<ProstyObiekt>> PobierzListe(string sql)
        {
            var lista = new List<ProstyObiekt>();
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lista.Add(new ProstyObiekt { Id = reader.GetInt32(0), Nazwa = reader.GetString(1) });
                    }
                }
            }
            return lista;
        }

        private async Task<DataTable> PobierzTabelaProsta(string sql)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }

        #endregion

        #region Autoryzacja

        public async Task<(bool, string)> ZalogujAdmina(string login, string haslo)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("SELECT fn_ZalogujAdmina(@login, @haslo)", conn))
                {
                    cmd.Parameters.AddWithValue("login", login);
                    cmd.Parameters.AddWithValue("haslo", haslo);

                    var wynik = await cmd.ExecuteScalarAsync();
                    if (wynik is bool sukces && sukces)
                    {
                        return (true, "Zalogowano pomyślnie");
                    }
                    return (false, "Błędny login lub hasło.");
                }
            }
        }

        #endregion

        #region Strefa Użytkownika (Wyszukiwanie i Rezerwacja)

        public async Task<List<WynikSeansu>> WyszukajSeanse(string tytul, int? idKategorii = null, DateTime? data = null)
        {
            var listaWynikow = new List<WynikSeansu>();
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("SELECT * FROM fn_WyszukajSeansePorownawczo(@tytul, @kat::int, @data::date)", conn))
                {
                    cmd.Parameters.AddWithValue("tytul", tytul);
                    cmd.Parameters.AddWithValue("kat", idKategorii.HasValue ? (object)idKategorii.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("data", data.HasValue ? (object)data.Value : DBNull.Value);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            listaWynikow.Add(new WynikSeansu
                            {
                                IdSeansu = reader.GetInt32(reader.GetOrdinal("id_seansu")),
                                TytulFilmu = reader.GetString(reader.GetOrdinal("tytul_filmu")),
                                Opis = reader.IsDBNull(reader.GetOrdinal("opis")) ? "" : reader.GetString(reader.GetOrdinal("opis")),
                                Kategoria = reader.GetString(reader.GetOrdinal("kategoria")),
                                Kino = reader.GetString(reader.GetOrdinal("kino")),
                                Sala = reader.GetString(reader.GetOrdinal("sala")),
                                DataGodzina = reader.GetDateTime(reader.GetOrdinal("data_godzina")),
                                Cena = reader.GetDecimal(reader.GetOrdinal("cena"))
                            });
                        }
                    }
                }
            }
            return listaWynikow;
        }

        public async Task<List<Miejsce>> PobierzMapeSali(int idSeansu)
        {
            var listaMiejsc = new List<Miejsce>();
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("SELECT * FROM fn_PobierzMapeSaliDlaSeansu(@id)", conn))
                {
                    cmd.Parameters.AddWithValue("id", idSeansu);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            listaMiejsc.Add(new Miejsce
                            {
                                IdMiejsca = reader.GetInt32(reader.GetOrdinal("id_miejsca")),
                                Rzad = reader.GetInt32(reader.GetOrdinal("rzad")),
                                Numer = reader.GetInt32(reader.GetOrdinal("numer_miejsca")),
                                Status = reader.GetString(reader.GetOrdinal("status"))
                            });
                        }
                    }
                }
            }
            return listaMiejsc;
        }

        public async Task ZrobRezerwacje(int idSeansu, List<int> idsMiejsc, string imie, string email)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("CALL sp_ZrobRezerwacje(@id_seansu, @lista_miejsc, @imie, @email)", conn))
                {
                    cmd.Parameters.AddWithValue("id_seansu", idSeansu);
                    cmd.Parameters.AddWithValue("lista_miejsc", idsMiejsc.ToArray());
                    cmd.Parameters.AddWithValue("imie", imie);
                    cmd.Parameters.AddWithValue("email", email);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        #endregion

        #region Admin: Słowniki (Listy rozwijane)

        public async Task<List<ProstyObiekt>> PobierzFilmyLista()
        {
            return await PobierzListe("SELECT id_filmu, tytul FROM Filmy ORDER BY tytul");
        }

        public async Task<List<ProstyObiekt>> PobierzKinaLista()
        {
            return await PobierzListe("SELECT id_kina, nazwa FROM Kina ORDER BY nazwa");
        }

        public async Task<List<ProstyObiekt>> PobierzKategorieLista()
        {
            return await PobierzListe("SELECT id_kategorii, nazwa FROM Kategorie ORDER BY nazwa");
        }

        public async Task<List<ProstyObiekt>> PobierzSaleLista(int idKina)
        {
            return await PobierzListe($"SELECT id_sali, nazwa_sali FROM Sale WHERE id_kina = {idKina}");
        }

        #endregion

        #region Admin: Zarządzanie Filmami

        public async Task<DataTable> PobierzFilmyTabela()
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                string sql = @"
                    SELECT f.id_filmu, f.tytul, k.nazwa AS kategoria, f.czas_trwania_min 
                    FROM Filmy f
                    JOIN Kategorie k ON f.id_kategorii = k.id_kategorii
                    ORDER BY f.id_filmu";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }

        public async Task DodajFilm(string tytul, string opis, int czas, string kategoria)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("CALL sp_DodajFilm(@t, @o, @c, @k)", conn))
                {
                    cmd.Parameters.AddWithValue("t", tytul);
                    cmd.Parameters.AddWithValue("o", opis);
                    cmd.Parameters.AddWithValue("c", czas);
                    cmd.Parameters.AddWithValue("k", kategoria);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task UsunFilm(int idFilmu)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("CALL sp_UsunFilm(@id)", conn))
                {
                    cmd.Parameters.AddWithValue("id", idFilmu);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        #endregion

        #region Admin: Zarządzanie Kinami i Salami

        public async Task<DataTable> PobierzKinaTabela()
        {
            return await PobierzTabelaProsta("SELECT * FROM Kina ORDER BY id_kina");
        }

        public async Task<int> DodajKino(string nazwa, string adres)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("SELECT fn_DodajKino(@n, @a)", conn))
                {
                    cmd.Parameters.AddWithValue("n", nazwa);
                    cmd.Parameters.AddWithValue("a", adres);
                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task UsunKino(int idKina)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("CALL sp_UsunKino(@id)", conn))
                {
                    cmd.Parameters.AddWithValue("id", idKina);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DodajSale(int idKina, string nazwaSali, int rzedy, int miejsca)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("CALL sp_DodajSale(@id, @nazwa, @rz, @mj)", conn))
                {
                    cmd.Parameters.AddWithValue("id", idKina);
                    cmd.Parameters.AddWithValue("nazwa", nazwaSali);
                    cmd.Parameters.AddWithValue("rz", rzedy);
                    cmd.Parameters.AddWithValue("mj", miejsca);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        #endregion

        #region Admin: Zarządzanie Seansami

        public async Task<DataTable> PobierzSeanseTabela()
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                string sql = @"
                    SELECT s.id_seansu, f.tytul, k.nazwa AS kino, sa.nazwa_sali, s.data_godzina_seansu, s.cena 
                    FROM Seanse s
                    JOIN Filmy f ON s.id_filmu = f.id_filmu
                    JOIN Sale sa ON s.id_sali = sa.id_sali
                    JOIN Kina k ON sa.id_kina = k.id_kina
                    ORDER BY s.data_godzina_seansu DESC";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }

        public async Task DodajSeans(int idFilmu, int idSali, DateTime data, decimal cena)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("CALL sp_DodajSeans(@f, @s, @d, @c)", conn))
                {
                    cmd.Parameters.AddWithValue("f", idFilmu);
                    cmd.Parameters.AddWithValue("s", idSali);
                    cmd.Parameters.AddWithValue("d", data);
                    cmd.Parameters.AddWithValue("c", cena);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task UsunSeans(int idSeansu)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("CALL sp_UsunSeans(@id)", conn))
                {
                    cmd.Parameters.AddWithValue("id", idSeansu);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        #endregion

        #region Admin: Zarządzanie Rezerwacjami

        public async Task<List<RezerwacjaAdmin>> PobierzRezerwacjeAdmin()
        {
            var lista = new List<RezerwacjaAdmin>();
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("SELECT * FROM fn_PobierzWszystkieRezerwacje()", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lista.Add(new RezerwacjaAdmin
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id_rezerwacji")),
                            Film = reader.GetString(reader.GetOrdinal("film")),
                            Kino = reader.GetString(reader.GetOrdinal("kino")),
                            Data = reader.GetDateTime(reader.GetOrdinal("data")),
                            Klient = reader.IsDBNull(reader.GetOrdinal("klient")) ? "" : reader.GetString(reader.GetOrdinal("klient")),
                            IloscMiejsc = reader.GetInt64(reader.GetOrdinal("ilosc_miejsc"))
                        });
                    }
                }
            }
            return lista;
        }

        public async Task UsunRezerwacje(int idRezerwacji)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("CALL sp_AnulujRezerwacje(@id)", conn))
                {
                    cmd.Parameters.AddWithValue("id", idRezerwacji);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        #endregion

        #region Admin: Statystyki

        public async Task<(int bilety, decimal kasa)> PobierzStatystykiGlobalne()
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("SELECT * FROM fn_StatystykiGlobalne()", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int bilety = reader.GetInt32(0);
                        decimal kasa = reader.GetDecimal(1);
                        return (bilety, kasa);
                    }
                }
            }
            return (0, 0);
        }

        public async Task<DataTable> PobierzRankingFilmow()
        {
            return await PobierzTabelaProsta("SELECT * FROM fn_StatystykiFilmy()");
        }

        public async Task<DataTable> PobierzRankingKategorii()
        {
            return await PobierzTabelaProsta("SELECT * FROM fn_StatystykiKategorie()");
        }

        #endregion
    }
}