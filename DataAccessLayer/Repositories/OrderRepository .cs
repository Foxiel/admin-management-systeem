using DataAccessLayer.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class OrderRepository : dbContext
    {
        public async Task<IEnumerable<Order>> GetFilteredAsync(string? klantNaam, string? klantEmail, string? klantTelefoon, DateTime? bestelDatum, string? bestelStatus)
        {
            var items = new List<Order>();

            var sql = @"
SELECT 
    b.bestelling_id,
    b.klant_id,
    b.order_datum,
    b.order_status,
    k.naam,
    k.telefoon,
    e.email
FROM Bestelling b
LEFT JOIN Klant k ON b.klant_id = k.klant_id
LEFT JOIN Account a ON a.klant_id = k.klant_id
LEFT JOIN Email e ON a.email_id = e.email_id
WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(klantNaam))
            {
                sql += " AND k.naam LIKE @klantNaam";
                parameters.Add(new SqlParameter("@klantNaam", $"%{klantNaam}%"));
            }

            if (!string.IsNullOrWhiteSpace(klantEmail))
            {
                sql += " AND e.email LIKE @klantEmail";
                parameters.Add(new SqlParameter("@klantEmail", $"%{klantEmail}%"));
            }

            if (!string.IsNullOrWhiteSpace(klantTelefoon))
            {
                sql += " AND k.telefoon LIKE @klantTelefoon";
                parameters.Add(new SqlParameter("@klantTelefoon", $"%{klantTelefoon}%"));
            }

            if (bestelDatum.HasValue)
            {
                sql += " AND CAST(b.order_datum AS date) = @bestelDatum";
                parameters.Add(new SqlParameter("@bestelDatum", bestelDatum.Value.Date));
            }

            if (!string.IsNullOrWhiteSpace(bestelStatus))
            {
                sql += " AND b.order_status = @bestelStatus";
                parameters.Add(new SqlParameter("@bestelStatus", bestelStatus));
            }

            sql += @"
ORDER BY
    CASE b.order_status
        WHEN 'Bestelling ontvangen' THEN 1
        WHEN 'Bestelling wordt gepicked' THEN 2
        WHEN 'Klaar voor verzending' THEN 3
        WHEN 'Onderweg' THEN 4
        WHEN 'Afgeleverd' THEN 5
        WHEN 'Vertraagd' THEN 6
        ELSE 7
    END,
    b.order_datum DESC,
    k.naam ASC";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);

            if (parameters.Count > 0)
                command.Parameters.AddRange(parameters.ToArray());

            await using var reader = await command.ExecuteReaderAsync();

            var orderIds = new List<int>();

            while (await reader.ReadAsync())
            {
                var order = new Order
                {
                    Id = ReadInt32Safe(reader, "bestelling_id"),
                    BestelDatum = ReadDateTimeSafe(reader, "order_datum"),
                    BestelStatus = ReadStringSafe(reader, "order_status"),
                    Klant = new Customer
                    {
                        Id = ReadInt32Safe(reader, "klant_id"),
                        Naam = ReadStringSafe(reader, "naam"),
                        Telefoonnr = ReadStringSafe(reader, "telefoon"),
                        Email = ReadStringSafe(reader, "email")
                    },
                    Bestelregels = new List<OrderItem>(),
                    Producten = new List<Product>()
                };

                items.Add(order);
                orderIds.Add(order.Id);
            }

            await reader.CloseAsync();

            await LoadOrderLinesAsync(connection, items, orderIds);

            return items;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            const string sql = @"
SELECT 
    b.bestelling_id,
    b.klant_id,
    b.order_datum,
    b.order_status,
    k.naam,
    k.telefoon,
    e.email
FROM Bestelling b
LEFT JOIN Klant k ON b.klant_id = k.klant_id
LEFT JOIN Account a ON a.klant_id = k.klant_id
LEFT JOIN Email e ON a.email_id = e.email_id
WHERE b.bestelling_id = @Id";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Order? order = null;

            if (await reader.ReadAsync())
            {
                order = new Order
                {
                    Id = ReadInt32Safe(reader, "bestelling_id"),
                    BestelDatum = ReadDateTimeSafe(reader, "order_datum"),
                    BestelStatus = ReadStringSafe(reader, "order_status"),
                    Klant = new Customer
                    {
                        Id = ReadInt32Safe(reader, "klant_id"),
                        Naam = ReadStringSafe(reader, "naam"),
                        Telefoonnr = ReadStringSafe(reader, "telefoon"),
                        Email = ReadStringSafe(reader, "email")
                    },
                    Bestelregels = new List<OrderItem>(),
                    Producten = new List<Product>()
                };
            }

            await reader.CloseAsync();

            if (order == null)
                return null;

            await LoadOrderLinesAsync(connection, new List<Order> { order }, new List<int> { id });

            return order;
        }

        private async Task LoadOrderLinesAsync(SqlConnection connection, List<Order> orders, List<int> orderIds)
        {
            if (orderIds.Count == 0)
                return;

            var inClause = string.Join(",", orderIds);

            var sql = $@"
SELECT 
    br.bestelling_id,
    br.product_id AS regel_product_id,
    br.aantal,
    p.product_id AS product_product_id,
    p.ean,
    p.naam,
    p.prijs,
    p.huidige_voorraad,
    p.minimum_voorraad,
    p.status,
    p.locatie_id,
    pl.gang,
    pl.schap,
    pl.vak
FROM Bestelregel br
LEFT JOIN Product p ON br.product_id = p.product_id
LEFT JOIN ProductLocatie pl ON p.locatie_id = pl.locatie_id
WHERE br.bestelling_id IN ({inClause})";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var linesByOrder = new Dictionary<int, List<OrderItem>>();

            while (await reader.ReadAsync())
            {
                var orderId = ReadInt32Safe(reader, "bestelling_id");
                var locatieNaam = $"{ReadStringSafe(reader, "gang")} {ReadStringSafe(reader, "schap")} {ReadStringSafe(reader, "vak")}".Trim();

                var item = new OrderItem
                {
                    ProductId = ReadInt32Safe(reader, "regel_product_id"),
                    Aantal = ReadInt32Safe(reader, "aantal"),
                    Product = new Product
                    {
                        ProductId = ReadInt32Safe(reader, "product_product_id"),
                        EAN = ReadStringSafe(reader, "ean"),
                        Naam = ReadStringSafe(reader, "naam"),
                        Prijs = ReadDecimalSafe(reader, "prijs"),
                        HuidigeVoorraad = ReadInt32Safe(reader, "huidige_voorraad"),
                        MinimumVoorraad = ReadInt32Safe(reader, "minimum_voorraad"),
                        Status = ReadStringSafe(reader, "status"),
                        LocatieId = ReadInt32Safe(reader, "locatie_id"),
                        Locatie = new Location
                        {
                            LocatieId = ReadInt32Safe(reader, "locatie_id"),
                            Naam = string.IsNullOrWhiteSpace(locatieNaam) ? "-" : locatieNaam
                        }
                    }
                };

                if (!linesByOrder.TryGetValue(orderId, out var list))
                {
                    list = new List<OrderItem>();
                    linesByOrder[orderId] = list;
                }

                list.Add(item);
            }

            await reader.CloseAsync();

            foreach (var order in orders)
            {
                if (linesByOrder.TryGetValue(order.Id, out var regels))
                {
                    order.Bestelregels = regels;
                    order.Producten = new List<Product>();

                    foreach (var regel in regels)
                    {
                        if (regel.Product != null)
                            order.Producten.Add(regel.Product);
                    }
                }
            }
        }

        public async Task<int> AddAsync(Order order)
        {
            const string sql = @"
INSERT INTO Bestelling 
(
    klant_id, 
    order_datum, 
    order_status
)
VALUES 
(
    @KlantId, 
    @OrderDatum, 
    @OrderStatus
);

SELECT CAST(SCOPE_IDENTITY() AS int);";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@KlantId", order.Klant?.Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@OrderDatum", order.BestelDatum == DateTime.MinValue ? DateTime.Now : order.BestelDatum);
            command.Parameters.AddWithValue("@OrderStatus", string.IsNullOrWhiteSpace(order.BestelStatus) ? "Bestelling ontvangen" : order.BestelStatus);

            var result = await command.ExecuteScalarAsync();

            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task UpdateAsync(Order order)
        {
            const string sql = @"
UPDATE Bestelling
SET 
    klant_id = @KlantId,
    order_datum = @OrderDatum,
    order_status = @OrderStatus
WHERE bestelling_id = @Id";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@KlantId", order.Klant?.Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@OrderDatum", order.BestelDatum == DateTime.MinValue ? DateTime.Now : order.BestelDatum);
            command.Parameters.AddWithValue("@OrderStatus", order.BestelStatus ?? string.Empty);
            command.Parameters.AddWithValue("@Id", order.Id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                const string deleteLinesSql = @"
DELETE FROM Bestelregel 
WHERE bestelling_id = @Id";

                await using var deleteLinesCommand = new SqlCommand(deleteLinesSql, connection, (SqlTransaction)transaction);
                deleteLinesCommand.Parameters.AddWithValue("@Id", id);
                await deleteLinesCommand.ExecuteNonQueryAsync();

                const string deleteOrderSql = @"
DELETE FROM Bestelling 
WHERE bestelling_id = @Id";

                await using var deleteOrderCommand = new SqlCommand(deleteOrderSql, connection, (SqlTransaction)transaction);
                deleteOrderCommand.Parameters.AddWithValue("@Id", id);
                await deleteOrderCommand.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateStatusAsync(int orderId, string status)
        {
            const string sql = @"
UPDATE Bestelling
SET order_status = @Status
WHERE bestelling_id = @OrderId";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@Status", status);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<SendOrder?> GetShipmentModelAsync(int bestellingId)
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
                    OrderStatus = ReadStringSafe(reader, "order_status"),

                    KlantNaam = ReadStringSafe(reader, "naam"),
                    KlantTelefoon = ReadStringSafe(reader, "telefoon"),
                    KlantEmail = ReadStringSafe(reader, "email"),

                    Adres = ReadStringSafe(reader, "adres"),
                    Postcode = ReadStringSafe(reader, "postcode"),
                    Woonplaats = ReadStringSafe(reader, "woonplaats"),
                    Land = ReadStringSafe(reader, "land"),

                    LeverDatum = DateTime.Today.AddDays(1),
                    LeverTijd = new TimeSpan(9, 0, 0),
                    TrackTraceCode = GenerateTrackTraceCode(bestellingId)
                };
            }

            await reader.CloseAsync();

            if (model != null)
            {
                model.Bezorgers = await GetBezorgersAsync();
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

        public async Task<List<DeliveryPlanningItem>> GetDeliveryPlanningAsync(int bezorgerId, DateTime leverDatum)
        {
            var planning = new List<DeliveryPlanningItem>();

            const string sql = @"
SELECT 
    bz.bestelling_id,
    bz.lever_datum,
    bz.lever_tijd,
    k.naam,
    ba.adres,
    ba.postcode,
    ba.woonplaats
FROM Bezorging bz
LEFT JOIN Bestelling b ON bz.bestelling_id = b.bestelling_id
LEFT JOIN Klant k ON b.klant_id = k.klant_id
LEFT JOIN BezorgAdres ba ON ba.klant_id = k.klant_id
WHERE bz.bezorger_id = @BezorgerId
AND CAST(bz.lever_datum AS date) = @LeverDatum
ORDER BY bz.lever_tijd ASC";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@BezorgerId", bezorgerId);
            command.Parameters.AddWithValue("@LeverDatum", leverDatum.Date);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                planning.Add(new DeliveryPlanningItem
                {
                    BestellingId = ReadInt32Safe(reader, "bestelling_id"),
                    LeverDatum = ReadDateTimeSafe(reader, "lever_datum"),
                    LeverTijd = ReadTimeSpanSafe(reader, "lever_tijd"),
                    KlantNaam = ReadStringSafe(reader, "naam"),
                    Adres = ReadStringSafe(reader, "adres"),
                    Postcode = ReadStringSafe(reader, "postcode"),
                    Woonplaats = ReadStringSafe(reader, "woonplaats")
                });
            }

            return planning;
        }

        public async Task CreateShipmentAsync(SendOrder model)
        {
            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                if (!long.TryParse(model.TrackTraceCode, out var trackTraceCode))
                    throw new Exception("Track & trace code mag alleen cijfers bevatten.");

                const string insertSql = @"
INSERT INTO Bezorging
(
    bestelling_id,
    bezorger_id,
    lever_datum,
    lever_tijd,
    track_trace_code
)
VALUES
(
    @BestellingId,
    @BezorgerId,
    @LeverDatum,
    @LeverTijd,
    @TrackTraceCode
);";

                await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);

                insertCommand.Parameters.AddWithValue("@BestellingId", model.BestellingId);
                insertCommand.Parameters.AddWithValue("@BezorgerId", model.BezorgerId);
                insertCommand.Parameters.AddWithValue("@LeverDatum", model.LeverDatum.Date);
                insertCommand.Parameters.AddWithValue("@LeverTijd", model.LeverTijd);
                insertCommand.Parameters.AddWithValue("@TrackTraceCode", trackTraceCode);

                await insertCommand.ExecuteNonQueryAsync();

                const string updateOrderSql = @"
UPDATE Bestelling
SET order_status = 'Onderweg'
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

                var val = reader.GetValue(ord);

                if (val is int i) return i;
                if (val is long l) return Convert.ToInt32(l);

                return int.TryParse(val?.ToString(), out var parsed) ? parsed : 0;
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

                var val = reader.GetValue(ord);

                if (val is DateTime dt) return dt;

                return DateTime.TryParse(val?.ToString(), out var parsed) ? parsed : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static TimeSpan ReadTimeSpanSafe(SqlDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ord)) return TimeSpan.Zero;

                var val = reader.GetValue(ord);

                if (val is TimeSpan ts) return ts;

                return TimeSpan.TryParse(val?.ToString(), out var parsed) ? parsed : TimeSpan.Zero;
            }
            catch
            {
                return TimeSpan.Zero;
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

                return decimal.TryParse(val?.ToString(), out var parsed) ? parsed : 0m;
            }
            catch
            {
                return 0m;
            }
        }
    }
}