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
        public string Status { get; set; } // "wolne" lub "zajete"
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
                                // Używamy nazw kolumn zwróconych przez funkcję SQL
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
                // Wywołujemy naszą funkcję SQL
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

        // Metoda do wywoływania procedury transakcyjnej
        public async Task ZrobRezerwacje(int idSeansu, List<int> idsMiejsc, string imie, string email)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                // Używamy komendy CALL do procedur składowanych
                using (var cmd = new NpgsqlCommand("CALL sp_ZrobRezerwacje(@id_seansu, @lista_miejsc, @imie, @email)", conn))
                {
                    cmd.Parameters.AddWithValue("id_seansu", idSeansu);
                    cmd.Parameters.AddWithValue("lista_miejsc", idsMiejsc.ToArray()); // Konwertujemy listę na tablicę dla SQL
                    cmd.Parameters.AddWithValue("imie", imie);
                    cmd.Parameters.AddWithValue("email", email);

                    // ExecuteNonQueryAsync używamy, gdy nie spodziewamy się tabeli z wynikami,
                    // a jedynie wykonania akcji (INSERT/UPDATE/CALL)
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }

}