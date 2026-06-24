using DataAccessLayer.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class SendOrderRepository : dbContext
    {
        public async Task<List<SendOrder>> GetOrdersToSendAsync()
        {
            var orders = new List<SendOrder>();

            const string sql = @"
SELECT 
    b.bestelling_id,
    b.klant_id,
    b.order_datum,
    b.order_status,
    k.naam,
    k.telefoon,
    e.email,
    ba.adres,
    ba.postcode,
    ba.woonplaats,
    ba.land
FROM Bestelling b
LEFT JOIN Klant k ON b.klant_id = k.klant_id
LEFT JOIN Account a ON a.klant_id = k.klant_id
LEFT JOIN Email e ON a.email_id = e.email_id
LEFT JOIN BezorgAdres ba ON ba.klant_id = k.klant_id
WHERE b.order_status = 0
ORDER BY b.order_datum ASC";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                orders.Add(new SendOrder
                {
                    BestellingId = ReadInt32Safe(reader, "bestelling_id"),
                    KlantId = ReadInt32Safe(reader, "klant_id"),
                    OrderDatum = ReadDateTimeSafe(reader, "order_datum"),
                    OrderStatus = "Nog niet verzonden",

                    KlantNaam = ReadStringSafe(reader, "naam"),
                    KlantTelefoon = ReadStringSafe(reader, "telefoon"),
                    KlantEmail = ReadStringSafe(reader, "email"),

                    Adres = ReadStringSafe(reader, "adres"),
                    Postcode = ReadStringSafe(reader, "postcode"),
                    Woonplaats = ReadStringSafe(reader, "woonplaats"),
                    Land = ReadStringSafe(reader, "land")
                });
            }

            return orders;
        }

        public async Task<SendOrder?> GetOrderForSendingAsync(int bestellingId)
        {
            const string sql = @"
SELECT 
    b.bestelling_id,
    b.klant_id,
    b.order_datum,
    b.order_status,
    k.naam,
    k.telefoon,
    e.email,
    ba.adres,
    ba.postcode,
    ba.woonplaats,
    ba.land
FROM Bestelling b
LEFT JOIN Klant k ON b.klant_id = k.klant_id
LEFT JOIN Account a ON a.klant_id = k.klant_id
LEFT JOIN Email e ON a.email_id = e.email_id
LEFT JOIN BezorgAdres ba ON ba.klant_id = k.klant_id
WHERE b.bestelling_id = @BestellingId";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@BestellingId", bestellingId);

            await using var reader = await command.ExecuteReaderAsync();

            SendOrder? model = null;

            if (await reader.ReadAsync())
            {
                model = new SendOrder
                {
                    BestellingId = ReadInt32Safe(reader, "bestelling_id"),
                    KlantId = ReadInt32Safe(reader, "klant_id"),
                    OrderDatum = ReadDateTimeSafe(reader, "order_datum"),
                    OrderStatus = "Nog niet verzonden",

                    KlantNaam = ReadStringSafe(reader, "naam"),
                    KlantTelefoon = ReadStringSafe(reader, "telefoon"),
                    KlantEmail = ReadStringSafe(reader, "email"),

                    Adres = ReadStringSafe(reader, "adres"),
                    Postcode = ReadStringSafe(reader, "postcode"),
                    Woonplaats = ReadStringSafe(reader, "woonplaats"),
                    Land = ReadStringSafe(reader, "land"),

                    LeverDatum = DateTime.Today,
                    LeverTijd = DateTime.Now.TimeOfDay,
                    Status = 1,
                    TrackTraceCode = GenerateTrackTraceCode(bestellingId)
                };
            }

            await reader.CloseAsync();

            if (model != null)
            {
                model.Bezorgers = await GetBezorgersAsync();
                model.Statussen = await GetBezorgStatussenAsync();
            }

            return model;
        }

        public async Task<List<BezorgerOption>> GetBezorgersAsync()
        {
            var bezorgers = new List<BezorgerOption>();

            const string sql = @"
SELECT 
    bezorger_id,
    naam
FROM Bezorger
ORDER BY naam";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                bezorgers.Add(new BezorgerOption
                {
                    Id = ReadInt32Safe(reader, "bezorger_id"),
                    Naam = ReadStringSafe(reader, "naam")
                });
            }

            return bezorgers;
        }

        public async Task<List<BezorgStatusOption>> GetBezorgStatussenAsync()
        {
            var statussen = new List<BezorgStatusOption>();

            const string sql = @"
SELECT 
    bezorgstatus_id,
    naam
FROM BezorgStatus
ORDER BY bezorgstatus_id";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                statussen.Add(new BezorgStatusOption
                {
                    Id = ReadInt32Safe(reader, "bezorgstatus_id"),
                    Naam = ReadStringSafe(reader, "naam")
                });
            }

            return statussen;
        }

        public async Task CreateShipmentAsync(SendOrder model)
        {
            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                if (!long.TryParse(model.TrackTraceCode, out var trackTraceCode))
                {
                    throw new Exception("Track & trace code mag alleen cijfers bevatten.");
                }

                const string insertSql = @"
INSERT INTO Bezorging
(
    bestelling_id,
    bezorger_id,
    lever_datum,
    lever_tijd,
    status,
    track_trace_code
)
VALUES
(
    @BestellingId,
    @BezorgerId,
    @LeverDatum,
    @LeverTijd,
    @Status,
    @TrackTraceCode
);";

                await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
                insertCommand.Parameters.AddWithValue("@BestellingId", model.BestellingId);
                insertCommand.Parameters.AddWithValue("@BezorgerId", model.BezorgerId);
                insertCommand.Parameters.AddWithValue("@LeverDatum", model.LeverDatum.Date);
                insertCommand.Parameters.AddWithValue("@LeverTijd", model.LeverTijd);
                insertCommand.Parameters.AddWithValue("@Status", "Onderweg");
                insertCommand.Parameters.AddWithValue("@TrackTraceCode", trackTraceCode);

                await insertCommand.ExecuteNonQueryAsync();

                const string updateOrderSql = @"
UPDATE Bestelling
SET order_status = 1
WHERE bestelling_id = @BestellingId";

                await using var updateCommand = new SqlCommand(updateOrderSql, connection, (SqlTransaction)transaction);
                updateCommand.Parameters.AddWithValue("@BestellingId", model.BestellingId);

                await updateCommand.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static string GenerateTrackTraceCode(int bestellingId)
        {
            return $"{DateTime.Now:yyyyMMdd}{bestellingId}";
        }

        private static int ReadInt32Safe(SqlDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ord)) return 0;

                var value = reader.GetValue(ord);

                if (value is int i) return i;
                if (value is long l) return Convert.ToInt32(l);

                return int.TryParse(value?.ToString(), out var parsed) ? parsed : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static string ReadStringSafe(SqlDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ord)) return string.Empty;

                return reader.GetValue(ord)?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static DateTime ReadDateTimeSafe(SqlDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ord)) return DateTime.MinValue;

                var value = reader.GetValue(ord);

                if (value is DateTime dt) return dt;

                return DateTime.TryParse(value?.ToString(), out var parsed)
                    ? parsed
                    : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}