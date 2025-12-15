using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace KinoRezerwator
{
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
        public string Kategoria { get; set; }
        public string Kino { get; set; }
        public string Sala { get; set; }
        public DateTime DataGodzina { get; set; }
        public decimal Cena { get; set; }

        public string OpisWyswietlania => $"{DataGodzina:HH:mm} | {Kino} ({Sala})";
    }

    public class ProstyObiekt { public int Id { get; set; } public string Nazwa { get; set; } }

    public class RezerwacjaAdmin
    {
        public int Id { get; set; }
        public string Film { get; set; }
        public string Kino { get; set; }
        public DateTime Data { get; set; }
        public string Klient { get; set; }
        public long IloscMiejsc { get; set; }
    }

    public class BazaDanych
    {
        private const string ConnectionString = "Host=localhost;Username=postgres;Password=admin;Database=KinoRezerwator";

        public async Task<List<WynikSeansu>> WyszukajSeanse(string tytul)
        {
            var listaWynikow = new List<WynikSeansu>();

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand("SELECT * FROM fn_WyszukajSeansePorownawczo(@tytul)", conn))
                {
                    cmd.Parameters.AddWithValue("tytul", tytul);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            listaWynikow.Add(new WynikSeansu
                            {
                                IdSeansu = reader.GetInt32(reader.GetOrdinal("id_seansu")),
                                TytulFilmu = reader.GetString(reader.GetOrdinal("tytul_filmu")),
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

        public async Task<bool> ZalogujAdmina(string login, string haslo)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand("SELECT fn_ZalogujAdmina(@login, @haslo)", conn))
                {
                    cmd.Parameters.AddWithValue("login", login);
                    cmd.Parameters.AddWithValue("haslo", haslo);

                    var wynik = await cmd.ExecuteScalarAsync();

                    if (wynik is bool sukces)
                    {
                        return sukces;
                    }
                    return false;
                }
            }
        }

        public async Task<List<ProstyObiekt>> PobierzFilmyLista()
        {
            return await PobierzListe("SELECT id_filmu, tytul FROM Filmy ORDER BY tytul");
        }

        public async Task<List<ProstyObiekt>> PobierzKinaLista()
        {
            return await PobierzListe("SELECT id_kina, nazwa FROM Kina ORDER BY nazwa");
        }

        public async Task<List<ProstyObiekt>> PobierzSaleLista(int idKina)
        {
            var lista = new List<ProstyObiekt>();
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand($"SELECT id_sali, nazwa_sali FROM Sale WHERE id_kina = {idKina}", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        lista.Add(new ProstyObiekt { Id = reader.GetInt32(0), Nazwa = reader.GetString(1) });
            }
            return lista;
        }

        private async Task<List<ProstyObiekt>> PobierzListe(string sql)
        {
            var lista = new List<ProstyObiekt>();
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        lista.Add(new ProstyObiekt { Id = reader.GetInt32(0), Nazwa = reader.GetString(1) });
            }
            return lista;
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
    }

}