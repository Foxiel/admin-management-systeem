using DataAccessLayer.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories;

public class ProductRepository : BaseDAL
{
    public ProductRepository(IConfiguration configuration) : base(configuration)
    {
    }

    private async Task<bool> TableExists(string tableName)
    {
        const string sql = @"
SELECT COUNT(*) 
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_NAME = @name";
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@name", tableName);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    private async Task<bool> ColumnExists(string tableName, string columnName)
    {
        const string sql = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = @table AND c.COLUMN_NAME = @column";
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@table", tableName);
        command.Parameters.AddWithValue("@column", columnName);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    // NOTE: locatieId removed; leverancier filter by name (leverancierNaam)
    public async Task<IEnumerable<Product>> GetFilteredAsync(
        string? ean,
        string? naam,
        string? leverancierNaam,
        decimal? minPrijs,
        decimal? maxPrijs,
        int? huidigeVoorraadMax,
        string? status)
    {
        var items = new List<Product>();

        // --- detecteer leverancier tabel/naam-kolom ---
        string? leverancierTable = await TableExists("leverancier") ? "leverancier"
            : await TableExists("leveranciers") ? "leveranciers" : null;

        string leverancierNameColumn = string.Empty;
        if (!string.IsNullOrEmpty(leverancierTable))
        {
            if (await ColumnExists(leverancierTable, "naam")) leverancierNameColumn = "naam";
            else if (await ColumnExists(leverancierTable, "leverancier_naam")) leverancierNameColumn = "leverancier_naam";
        }

        // --- detecteer locatie tabel en kolommen (ProductLocatie preferred) ---
        string? locatieTable = await TableExists("ProductLocatie") ? "ProductLocatie"
            : await TableExists("locatie") ? "locatie"
            : await TableExists("locaties") ? "locaties" : null;

        var locatieHasNaam = false;
        var locatieHasGang = false;
        var locatieHasSchap = false;
        var locatieHasVak = false;
        if (!string.IsNullOrEmpty(locatieTable))
        {
            locatieHasNaam = await ColumnExists(locatieTable, "naam");
            locatieHasGang = await ColumnExists(locatieTable, "gang");
            locatieHasSchap = await ColumnExists(locatieTable, "schap");
            locatieHasVak = await ColumnExists(locatieTable, "vak");
        }

        // Bouw SELECT-kolommen zonder trailing comma issues
        var selectColumns = new List<string>
        {
            "p.product_id",
            "p.ean",
            "p.leverancier_id",
            "p.locatie_id",
            "p.naam",
            "p.beschrijving",
            "p.prijs",
            "p.gewicht",
            "p.garantie",
            "p.huidige_voorraad",
            "p.minimum_voorraad",
            "p.status"
        };

        if (!string.IsNullOrEmpty(leverancierTable) && !string.IsNullOrEmpty(leverancierNameColumn))
            selectColumns.Add($"l.{leverancierNameColumn} AS leveranciernaam");
        else
            selectColumns.Add("NULL AS leveranciernaam");

        if (!string.IsNullOrEmpty(locatieTable))
        {
            selectColumns.Add(locatieHasNaam ? "loc.naam AS locatienaam" : "NULL AS locatienaam");
            if (locatieHasGang) selectColumns.Add("loc.gang");
            if (locatieHasSchap) selectColumns.Add("loc.schap");
            if (locatieHasVak) selectColumns.Add("loc.vak");
        }
        else
        {
            selectColumns.Add("NULL AS locatienaam");
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("SELECT");
        sb.AppendLine(string.Join(",\n", selectColumns));
        sb.AppendLine("FROM product p");

        if (!string.IsNullOrEmpty(leverancierTable))
            sb.AppendLine($"LEFT JOIN {leverancierTable} l ON p.leverancier_id = l.leverancier_id");
        if (!string.IsNullOrEmpty(locatieTable))
            sb.AppendLine($"LEFT JOIN {locatieTable} loc ON p.locatie_id = loc.locatie_id");

        sb.AppendLine("WHERE 1=1");

        var sql = sb.ToString();
        var parameters = new List<SqlParameter>();

        if (!string.IsNullOrWhiteSpace(ean))
        {
            sql += " AND p.ean LIKE @ean";
            parameters.Add(new SqlParameter("@ean", $"%{ean}%"));
        }

        if (!string.IsNullOrWhiteSpace(naam))
        {
            sql += " AND p.naam LIKE @naam";
            parameters.Add(new SqlParameter("@naam", $"%{naam}%"));
        }

        // supplier name filter (uses joined supplier name column when available)
        if (!string.IsNullOrWhiteSpace(leverancierNaam) && !string.IsNullOrEmpty(leverancierNameColumn))
        {
            sql += $" AND l.{leverancierNameColumn} LIKE @leverancierNaam";
            parameters.Add(new SqlParameter("@leverancierNaam", $"%{leverancierNaam}%"));
        }

        if (minPrijs.HasValue)
        {
            sql += " AND p.prijs >= @minPrijs";
            parameters.Add(new SqlParameter("@minPrijs", minPrijs.Value));
        }

        if (maxPrijs.HasValue)
        {
            sql += " AND p.prijs <= @maxPrijs";
            parameters.Add(new SqlParameter("@maxPrijs", maxPrijs.Value));
        }

        if (huidigeVoorraadMax.HasValue)
        {
            sql += " AND p.huidige_voorraad <= @huidigeVoorraadMax";
            parameters.Add(new SqlParameter("@huidigeVoorraadMax", huidigeVoorraadMax.Value));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            sql += " AND p.status = @status";
            parameters.Add(new SqlParameter("@status", status));
        }

        sql += " ORDER BY p.naam";

        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        if (parameters.Count > 0) command.Parameters.AddRange(parameters.ToArray());

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var p = new Product
            {
                ProductId = ReadInt32Safe(reader, "product_id"),
                EAN = ReadStringSafe(reader, "ean"),
                LeverancierId = ReadInt32Safe(reader, "leverancier_id"),
                LocatieId = ReadInt32Safe(reader, "locatie_id"),
                Naam = ReadStringSafe(reader, "naam"),
                Beschrijving = ReadStringSafe(reader, "beschrijving"),
                Prijs = ReadDecimalSafe(reader, "prijs"),
                Gewicht = ReadDecimalSafe(reader, "gewicht"),
                Garantie = ReadStringSafe(reader, "garantie"),
                HuidigeVoorraad = ReadInt32Safe(reader, "huidige_voorraad"),
                MinimumVoorraad = ReadInt32Safe(reader, "minimum_voorraad"),
                Status = ReadStringSafe(reader, "status")
            };

            var leverancierNaamVal = ReadStringSafe(reader, "leveranciernaam");
            if (!string.IsNullOrEmpty(leverancierNaamVal))
                p.Leverancier = new Manufacturer { LeverancierId = p.LeverancierId, Naam = leverancierNaamVal };

            string locatieNaam = ReadStringSafe(reader, "locatienaam");
            if (string.IsNullOrEmpty(locatieNaam))
            {
                var parts = new List<string>();
                if (locatieHasGang) { var g = ReadStringSafe(reader, "gang"); if (!string.IsNullOrWhiteSpace(g)) parts.Add(g); }
                if (locatieHasSchap) { var s = ReadStringSafe(reader, "schap"); if (!string.IsNullOrWhiteSpace(s)) parts.Add(s); }
                if (locatieHasVak) { var v = ReadStringSafe(reader, "vak"); if (!string.IsNullOrWhiteSpace(v)) parts.Add(v); }
                locatieNaam = string.Join(" ", parts);
            }
            if (!string.IsNullOrEmpty(locatieNaam))
                p.Locatie = new Location { LocatieId = p.LocatieId, Naam = locatieNaam };

            items.Add(p);
        }

        return items;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        // detect leverancier table/name column
        string? leverancierTable = await TableExists("leverancier") ? "leverancier"
            : await TableExists("leveranciers") ? "leveranciers" : null;

        string leverancierNameColumn = string.Empty;
        if (!string.IsNullOrEmpty(leverancierTable))
        {
            if (await ColumnExists(leverancierTable, "naam")) leverancierNameColumn = "naam";
            else if (await ColumnExists(leverancierTable, "leverancier_naam")) leverancierNameColumn = "leverancier_naam";
        }

        // detect locatie table and columns
        string? locatieTable = await TableExists("ProductLocatie") ? "ProductLocatie"
            : await TableExists("locatie") ? "locatie"
            : await TableExists("locaties") ? "locaties" : null;

        var locatieHasNaam = false;
        var locatieHasGang = false;
        var locatieHasSchap = false;
        var locatieHasVak = false;
        if (!string.IsNullOrEmpty(locatieTable))
        {
            locatieHasNaam = await ColumnExists(locatieTable, "naam");
            locatieHasGang = await ColumnExists(locatieTable, "gang");
            locatieHasSchap = await ColumnExists(locatieTable, "schap");
            locatieHasVak = await ColumnExists(locatieTable, "vak");
        }

        var selectCols = new List<string>
        {
            "p.product_id",
            "p.ean",
            "p.leverancier_id",
            "p.locatie_id",
            "p.naam",
            "p.beschrijving",
            "p.prijs",
            "p.gewicht",
            "p.garantie",
            "p.huidige_voorraad",
            "p.minimum_voorraad",
            "p.status"
        };

        if (!string.IsNullOrEmpty(leverancierTable) && !string.IsNullOrEmpty(leverancierNameColumn))
            selectCols.Add($"l.{leverancierNameColumn} AS leveranciernaam");
        else
            selectCols.Add("NULL AS leveranciernaam");

        if (!string.IsNullOrEmpty(locatieTable))
        {
            selectCols.Add(locatieHasNaam ? "loc.naam AS locatienaam" : "NULL AS locatienaam");
            if (locatieHasGang) selectCols.Add("loc.gang");
            if (locatieHasSchap) selectCols.Add("loc.schap");
            if (locatieHasVak) selectCols.Add("loc.vak");
        }
        else
        {
            selectCols.Add("NULL AS locatienaam");
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("SELECT");
        sb.AppendLine(string.Join(",\n", selectCols));
        sb.AppendLine("FROM product p");

        if (!string.IsNullOrEmpty(leverancierTable))
            sb.AppendLine($"LEFT JOIN {leverancierTable} l ON p.leverancier_id = l.leverancier_id");
        if (!string.IsNullOrEmpty(locatieTable))
            sb.AppendLine($"LEFT JOIN {locatieTable} loc ON p.locatie_id = loc.locatie_id");

        sb.AppendLine("WHERE p.product_id = @id");

        var sql = sb.ToString();

        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var p = new Product
            {
                ProductId = ReadInt32Safe(reader, "product_id"),
                EAN = ReadStringSafe(reader, "ean"),
                LeverancierId = ReadInt32Safe(reader, "leverancier_id"),
                LocatieId = ReadInt32Safe(reader, "locatie_id"),
                Naam = ReadStringSafe(reader, "naam"),
                Beschrijving = ReadStringSafe(reader, "beschrijving"),
                Prijs = ReadDecimalSafe(reader, "prijs"),
                Gewicht = ReadDecimalSafe(reader, "gewicht"),
                Garantie = ReadStringSafe(reader, "garantie"),
                HuidigeVoorraad = ReadInt32Safe(reader, "huidige_voorraad"),
                MinimumVoorraad = ReadInt32Safe(reader, "minimum_voorraad"),
                Status = ReadStringSafe(reader, "status")
            };

            var leverancierNaam = ReadStringSafe(reader, "leveranciernaam");
            if (!string.IsNullOrEmpty(leverancierNaam))
                p.Leverancier = new Manufacturer { LeverancierId = p.LeverancierId, Naam = leverancierNaam };

            string locatieNaam = ReadStringSafe(reader, "locatienaam");
            if (string.IsNullOrEmpty(locatieNaam))
            {
                var parts = new List<string>();
                if (locatieHasGang) { var g = ReadStringSafe(reader, "gang"); if (!string.IsNullOrWhiteSpace(g)) parts.Add(g); }
                if (locatieHasSchap) { var s = ReadStringSafe(reader, "schap"); if (!string.IsNullOrWhiteSpace(s)) parts.Add(s); }
                if (locatieHasVak) { var v = ReadStringSafe(reader, "vak"); if (!string.IsNullOrWhiteSpace(v)) parts.Add(v); }
                locatieNaam = string.Join(" ", parts);
            }
            if (!string.IsNullOrEmpty(locatieNaam))
                p.Locatie = new Location { LocatieId = p.LocatieId, Naam = locatieNaam };

            return p;
        }

        return null;
    }

    public async Task<int> AddAsync(Product model)
    {
        var sql = @"
INSERT INTO product (ean, leverancier_id, locatie_id, naam, beschrijving, prijs, gewicht, garantie, huidige_voorraad, minimum_voorraad, status)
VALUES (@ean, @leverancierId, @locatieId, @naam, @beschrijving, @prijs, @gewicht, @garantie, @huidige_voorraad, @minimum_voorraad, @status);
SELECT CAST(SCOPE_IDENTITY() AS int);
";
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ean", model.EAN ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@leverancierId", model.LeverancierId);
        command.Parameters.AddWithValue("@locatieId", model.LocatieId);
        command.Parameters.AddWithValue("@naam", model.Naam ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@beschrijving", model.Beschrijving ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@prijs", model.Prijs);
        command.Parameters.AddWithValue("@gewicht", model.Gewicht);
        command.Parameters.AddWithValue("@garantie", model.Garantie ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@huidige_voorraad", model.HuidigeVoorraad);
        command.Parameters.AddWithValue("@minimum_voorraad", model.MinimumVoorraad);
        command.Parameters.AddWithValue("@status", model.Status ?? (object)DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return result != null && int.TryParse(result.ToString(), out var newId) ? newId : 0;
    }

    public async Task UpdateAsync(Product model)
    {
        var sql = @"
UPDATE product
SET ean = @ean,
    leverancier_id = @leverancierId,
    locatie_id = @locatieId,
    naam = @naam,
    beschrijving = @beschrijving,
    prijs = @prijs,
   gewicht = @gewicht,
    garantie = @garantie,
    huidige_voorraad = @huidige_voorraad,
    minimum_voorraad = @minimum_voorraad,
    status = @status
WHERE product_id = @id";
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", model.ProductId);
        command.Parameters.AddWithValue("@ean", model.EAN ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@leverancierId", model.LeverancierId);
        command.Parameters.AddWithValue("@locatieId", model.LocatieId);
        command.Parameters.AddWithValue("@naam", model.Naam ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@beschrijving", model.Beschrijving ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@prijs", model.Prijs);
        command.Parameters.AddWithValue("@gewicht", model.Gewicht);
        command.Parameters.AddWithValue("@garantie", model.Garantie ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@huidige_voorraad", model.HuidigeVoorraad);
        command.Parameters.AddWithValue("@minimum_voorraad", model.MinimumVoorraad);
        command.Parameters.AddWithValue("@status", model.Status ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM product WHERE product_id = @id";
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<Manufacturer>> GetLeveranciersAsync()
    {
        var list = new List<Manufacturer>();
        var table = await TableExists("leverancier") ? "leverancier" : (await TableExists("leveranciers") ? "leveranciers" : null);
        if (table == null) return list;

        var sql = $"SELECT leverancier_id, naam FROM {table} ORDER BY naam";
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Manufacturer
            {
                LeverancierId = reader.IsDBNull(reader.GetOrdinal("leverancier_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("leverancier_id")),
                Naam = reader.IsDBNull(reader.GetOrdinal("naam")) ? string.Empty : reader.GetString(reader.GetOrdinal("naam"))
            });
        }
        return list;
    }

    public async Task<List<Location>> GetLocatiesAsync()
    {
        var list = new List<Location>();
        // prefer ProductLocatie, then locatie/locaties
        var table = await TableExists("ProductLocatie") ? "ProductLocatie" :
                    (await TableExists("locatie") ? "locatie" : (await TableExists("locaties") ? "locaties" : null));
        if (table == null) return list;

        // if table has a 'naam' column return that; otherwise read gang/schap/vak and compose name
        var hasNaam = await ColumnExists(table, "naam");
        if (hasNaam)
        {
            var sql = $"SELECT locatie_id, naam FROM {table} ORDER BY naam";
            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Location
                {
                    LocatieId = reader.IsDBNull(reader.GetOrdinal("locatie_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("locatie_id")),
                    Naam = reader.IsDBNull(reader.GetOrdinal("naam")) ? string.Empty : reader.GetString(reader.GetOrdinal("naam"))
                });
            }
            return list;
        }

        // fallback: compose from gang/schap/vak
        var sqlFallback = $"SELECT locatie_id, gang, schap, vak FROM {table} ORDER BY gang, schap, vak";
        await using var conn2 = (SqlConnection)GetConnection();
        await conn2.OpenAsync();
        await using var cmd2 = new SqlCommand(sqlFallback, conn2);
        await using var rdr2 = await cmd2.ExecuteReaderAsync();
        while (await rdr2.ReadAsync())
        {
            var gang = rdr2.IsDBNull(rdr2.GetOrdinal("gang")) ? string.Empty : rdr2.GetString(rdr2.GetOrdinal("gang"));
            var schap = rdr2.IsDBNull(rdr2.GetOrdinal("schap")) ? string.Empty : rdr2.GetString(rdr2.GetOrdinal("schap"));
            var vak = rdr2.IsDBNull(rdr2.GetOrdinal("vak")) ? string.Empty : rdr2.GetString(rdr2.GetOrdinal("vak"));
            var composed = string.Join(" ", new[] { gang, schap, vak }.Where(s => !string.IsNullOrWhiteSpace(s)));
            list.Add(new Location
            {
                LocatieId = rdr2.IsDBNull(rdr2.GetOrdinal("locatie_id")) ? 0 : rdr2.GetInt32(rdr2.GetOrdinal("locatie_id")),
                Naam = composed
            });
        }

        return list;
    }

    public async Task<string> GetLeverancierNaamAsync(int leverancierId)
    {
        if (leverancierId == 0) return string.Empty;
        var sql = "SELECT naam FROM leverancier WHERE leverancier_id = @id";
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", leverancierId);
        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? string.Empty;
    }

    public async Task<string> GetLocatieNaamAsync(int locatieId)
    {
        if (locatieId == 0) return string.Empty;

        // prefer ProductLocatie then locatie/locaties
        var table = await TableExists("ProductLocatie") ? "ProductLocatie" :
                    (await TableExists("locatie") ? "locatie" : (await TableExists("locaties") ? "locaties" : null));
        if (table == null) return string.Empty;
            
        var hasNaam = await ColumnExists(table, "naam");
        if (hasNaam)
        {
            var sql = $"SELECT naam FROM {table} WHERE locatie_id = @id";
            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", locatieId);
            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? string.Empty;
        }

        // fallback: compose
        var sqlFallback = $"SELECT gang, schap, vak FROM {table} WHERE locatie_id = @id";
        await using var conn = (SqlConnection)GetConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sqlFallback, conn);
        cmd.Parameters.AddWithValue("@id", locatieId);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var gang = reader.IsDBNull(reader.GetOrdinal("gang")) ? string.Empty : reader.GetString(reader.GetOrdinal("gang"));
            var schap = reader.IsDBNull(reader.GetOrdinal("schap")) ? string.Empty : reader.GetString(reader.GetOrdinal("schap"));
            var vak = reader.IsDBNull(reader.GetOrdinal("vak")) ? string.Empty : reader.GetString(reader.GetOrdinal("vak"));
            return string.Join(" ", new[] { gang, schap, vak }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        return string.Empty;
    }

    // Helper methods (add these as private methods in the same class)
    private static string ReadStringSafe(SqlDataReader reader, string columnName)
    {
        try
        {
            var ord = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ord)) return string.Empty;
            var val = reader.GetValue(ord);
            return val?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static int ReadInt32Safe(SqlDataReader reader, string columnName)
    {
        try
        {
            var ord = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ord)) return 0;
            var val = reader.GetValue(ord);
            // handle ints or long (bigint)
            if (val is int i) return i;
            if (val is long l) return Convert.ToInt32(l);
            if (int.TryParse(val?.ToString(), out var parsed)) return parsed;
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private static decimal ReadDecimalSafe(SqlDataReader reader, string columnName)
    {
        try
        {
            var ord = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ord)) return 0m;
            var val = reader.GetValue(ord);
            if (val is decimal d) return d;
            if (val is double db) return Convert.ToDecimal(db);
            if (val is float f) return Convert.ToDecimal(f);
            if (val is int i) return Convert.ToDecimal(i);
            if (val is long l) return Convert.ToDecimal(l);
            if (decimal.TryParse(val?.ToString(), out var parsed)) return parsed;
            return 0m;
        }
        catch
        {
            return 0m;
        }
    }

    public async Task<int> GetOrCreateLeverancierByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;

        // determine leverancier table
        var table = await TableExists("leverancier") ? "leverancier" : (await TableExists("leveranciers") ? "leveranciers" : null);
        if (table == null) return 0;

        var sqlSelect = $"SELECT leverancier_id FROM {table} WHERE naam = @name";
        await using var conn = (SqlConnection)GetConnection();
        await conn.OpenAsync();
        await using (var cmd = new SqlCommand(sqlSelect, conn))
        {
            cmd.Parameters.AddWithValue("@name", name);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null && int.TryParse(result.ToString(), out var id)) return id;
        }

        // not found -> insert
        var sqlInsert = $"INSERT INTO {table} (naam) VALUES (@name); SELECT CAST(SCOPE_IDENTITY() AS int);";
        await using var conn2 = (SqlConnection)GetConnection();
        await conn2.OpenAsync();
        await using var cmd2 = new SqlCommand(sqlInsert, conn2);
        cmd2.Parameters.AddWithValue("@name", name);
        var inserted = await cmd2.ExecuteScalarAsync();
        return inserted != null && int.TryParse(inserted.ToString(), out var newId) ? newId : 0;
    }

    public async Task<int> GetOrCreateLocatieByGangSchapVakAsync(string gang, string schap, string vak)
    {
        // determine locatie table
        var table = await TableExists("ProductLocatie") ? "ProductLocatie" :
                    (await TableExists("locatie") ? "locatie" : (await TableExists("locaties") ? "locaties" : null));
        if (table == null) return 0;

        // prefer match on gang+schap+vak if those columns exist
        var hasGang = await ColumnExists(table, "gang");
        var hasSchap = await ColumnExists(table, "schap");
        var hasVak = await ColumnExists(table, "vak");

        if (hasGang || hasSchap || hasVak)
        {
            var whereParts = new List<string>();
            if (hasGang) whereParts.Add("gang = @gang");
            if (hasSchap) whereParts.Add("schap = @schap");
            if (hasVak) whereParts.Add("vak = @vak");

            var sqlSelect = $"SELECT locatie_id FROM {table} WHERE {string.Join(" AND ", whereParts)}";
            await using var conn = (SqlConnection)GetConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sqlSelect, conn);
            if (hasGang) cmd.Parameters.AddWithValue("@gang", gang ?? (object)DBNull.Value);
            if (hasSchap) cmd.Parameters.AddWithValue("@schap", schap ?? (object)DBNull.Value);
            if (hasVak) cmd.Parameters.AddWithValue("@vak", vak ?? (object)DBNull.Value);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null && int.TryParse(result.ToString(), out var id)) return id;

            // insert new record if possible (only set columns that exist)
            var insertCols = new List<string>();
            var insertVals = new List<string>();
            if (hasGang) { insertCols.Add("gang"); insertVals.Add("@gang"); }
            if (hasSchap) { insertCols.Add("schap"); insertVals.Add("@schap"); }
            if (hasVak) { insertCols.Add("vak"); insertVals.Add("@vak"); }

            if (insertCols.Count == 0) return 0;

            var sqlInsert = $"INSERT INTO {table} ({string.Join(",", insertCols)}) VALUES ({string.Join(",", insertVals)}); SELECT CAST(SCOPE_IDENTITY() AS int);";
            await using var conn2 = (SqlConnection)GetConnection();
            await conn2.OpenAsync();
            await using var cmd2 = new SqlCommand(sqlInsert, conn2);
            if (hasGang) cmd2.Parameters.AddWithValue("@gang", gang ?? (object)DBNull.Value);
            if (hasSchap) cmd2.Parameters.AddWithValue("@schap", schap ?? (object)DBNull.Value);
            if (hasVak) cmd2.Parameters.AddWithValue("@vak", vak ?? (object)DBNull.Value);
            var inserted = await cmd2.ExecuteScalarAsync();
            return inserted != null && int.TryParse(inserted.ToString(), out var newId) ? newId : 0;
        }

        return 0;
    }
}

