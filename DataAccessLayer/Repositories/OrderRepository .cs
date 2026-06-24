using DataAccessLayer.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class OrderRepository : dbContext
    {
        public OrderRepository()
        {
        }

        public async Task<IEnumerable<Order>> GetFilteredAsync(
            string? klantNaam,
            string? klantEmail,
            string? klantTelefoon,
            DateTime? bestelDatum,
            string? bestelStatus)
        {
            var items = new List<Order>();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("SELECT b.bestelling_id, b.klant_id, b.order_datum, b.order_status,");
            sb.AppendLine("k.naam, k.telefoon, e.email");
            sb.AppendLine("FROM Bestelling b");
            sb.AppendLine("LEFT JOIN klant k ON b.klant_id = k.klant_id");
            sb.AppendLine("LEFT JOIN Account a ON a.klant_id = k.klant_id");
            sb.AppendLine("LEFT JOIN Email e ON a.email_id = e.email_id");
            sb.AppendLine("WHERE 1=1");

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(klantNaam))
            {
                sb.AppendLine(" AND k.naam LIKE @klantNaam");
                parameters.Add(new SqlParameter("@klantNaam", $"%{klantNaam}%"));
            }

            if (!string.IsNullOrWhiteSpace(klantEmail))
            {
                sb.AppendLine(" AND e.email LIKE @klantEmail");
                parameters.Add(new SqlParameter("@klantEmail", $"%{klantEmail}%"));
            }

            if (!string.IsNullOrWhiteSpace(klantTelefoon))
            {
                sb.AppendLine(" AND k.telefoon LIKE @klantTelefoon");
                parameters.Add(new SqlParameter("@klantTelefoon", $"%{klantTelefoon}%"));
            }

            if (bestelDatum.HasValue)
            {
                sb.AppendLine(" AND CAST(b.order_datum AS date) = @bestelDatum");
                parameters.Add(new SqlParameter("@bestelDatum", bestelDatum.Value.Date));
            }

            if (!string.IsNullOrWhiteSpace(bestelStatus))
            {
                sb.AppendLine(" AND b.order_status LIKE @bestelStatus");
                parameters.Add(new SqlParameter("@bestelStatus", $"%{bestelStatus}%"));
            }

            sb.AppendLine("ORDER BY b.order_datum DESC");

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sb.ToString(), connection);
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

            if (orderIds.Count > 0)
            {
                var inClause = string.Join(",", orderIds);

                var sqlLines = $@"
SELECT 
    br.bestelling_id,
    br.product_id AS regel_product_id,
    br.aantal,
    p.product_id AS product_product_id,
    p.ean,
    p.naam,
    p.prijs
FROM Bestelregel br
LEFT JOIN product p ON br.product_id = p.product_id
WHERE br.bestelling_id IN ({inClause})";

                await using var cmd2 = new SqlCommand(sqlLines, connection);
                await using var rdr2 = await cmd2.ExecuteReaderAsync();

                var linesByOrder = new Dictionary<int, List<OrderItem>>();

                while (await rdr2.ReadAsync())
                {
                    var orderId = ReadInt32Safe(rdr2, "bestelling_id");

                    var item = new OrderItem
                    {
                        ProductId = ReadInt32Safe(rdr2, "regel_product_id"),
                        Aantal = ReadInt32Safe(rdr2, "aantal"),
                        Product = new Product
                        {
                            ProductId = ReadInt32Safe(rdr2, "product_product_id"),
                            EAN = ReadStringSafe(rdr2, "ean"),
                            Naam = ReadStringSafe(rdr2, "naam"),
                            Prijs = ReadDecimalSafe(rdr2, "prijs")
                        }
                    };

                    if (!linesByOrder.TryGetValue(orderId, out var list))
                    {
                        list = new List<OrderItem>();
                        linesByOrder[orderId] = list;
                    }

                    list.Add(item);
                }

                foreach (var order in items)
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

                await rdr2.CloseAsync();
            }

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
LEFT JOIN klant k ON b.klant_id = k.klant_id
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

            const string sqlLines = @"
SELECT 
    br.product_id AS regel_product_id,
    br.aantal,
    p.product_id AS product_product_id,
    p.ean,
    p.naam,
    p.prijs
FROM Bestelregel br
LEFT JOIN product p ON br.product_id = p.product_id
WHERE br.bestelling_id = @Id";

            await using var cmd2 = new SqlCommand(sqlLines, connection);
            cmd2.Parameters.AddWithValue("@Id", id);

            await using var rdr2 = await cmd2.ExecuteReaderAsync();

            var regels = new List<OrderItem>();

            while (await rdr2.ReadAsync())
            {
                var item = new OrderItem
                {
                    ProductId = ReadInt32Safe(rdr2, "regel_product_id"),
                    Aantal = ReadInt32Safe(rdr2, "aantal"),
                    Product = new Product
                    {
                        ProductId = ReadInt32Safe(rdr2, "product_product_id"),
                        EAN = ReadStringSafe(rdr2, "ean"),
                        Naam = ReadStringSafe(rdr2, "naam"),
                        Prijs = ReadDecimalSafe(rdr2, "prijs")
                    }
                };

                regels.Add(item);
            }

            await rdr2.CloseAsync();

            order.Bestelregels = regels;
            order.Producten = new List<Product>();

            foreach (var regel in regels)
            {
                if (regel.Product != null)
                    order.Producten.Add(regel.Product);
            }

            return order;
        }

        public async Task<int> AddAsync(Order order)
        {
            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                const string insertOrderSql = @"
INSERT INTO Bestelling (klant_id, order_datum, order_status)
VALUES (@KlantId, @OrderDatum, @OrderStatus);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                await using var cmd = new SqlCommand(insertOrderSql, connection, (SqlTransaction)transaction);
                cmd.Parameters.AddWithValue("@KlantId", order.Klant?.Id ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@OrderDatum", order.BestelDatum == DateTime.MinValue ? DateTime.UtcNow : order.BestelDatum);
                cmd.Parameters.AddWithValue("@OrderStatus", order.BestelStatus ?? (object)DBNull.Value);

                var inserted = await cmd.ExecuteScalarAsync();
                var newOrderId = inserted != null ? Convert.ToInt32(inserted) : 0;

                if (newOrderId > 0)
                {
                    if (order.Bestelregels != null && order.Bestelregels.Count > 0)
                    {
                        foreach (var regel in order.Bestelregels)
                        {
                            const string insertLine = @"
INSERT INTO Bestelregel (bestelling_id, product_id, aantal)
VALUES (@OrderId, @ProductId, @Aantal);";

                            await using var lineCmd = new SqlCommand(insertLine, connection, (SqlTransaction)transaction);
                            lineCmd.Parameters.AddWithValue("@OrderId", newOrderId);
                            lineCmd.Parameters.AddWithValue("@ProductId", regel.ProductId);
                            lineCmd.Parameters.AddWithValue("@Aantal", regel.Aantal <= 0 ? 1 : regel.Aantal);

                            await lineCmd.ExecuteNonQueryAsync();
                        }
                    }
                    else if (order.Producten != null && order.Producten.Count > 0)
                    {
                        foreach (var product in order.Producten)
                        {
                            const string insertLine = @"
INSERT INTO Bestelregel (bestelling_id, product_id, aantal)
VALUES (@OrderId, @ProductId, @Aantal);";

                            await using var lineCmd = new SqlCommand(insertLine, connection, (SqlTransaction)transaction);
                            lineCmd.Parameters.AddWithValue("@OrderId", newOrderId);
                            lineCmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                            lineCmd.Parameters.AddWithValue("@Aantal", 1);

                            await lineCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                await transaction.CommitAsync();
                return newOrderId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(Order order)
        {
            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                const string updateOrderSql = @"
UPDATE Bestelling
SET klant_id = @KlantId,
    order_datum = @OrderDatum,
    order_status = @OrderStatus
WHERE bestelling_id = @Id";

                await using var cmd = new SqlCommand(updateOrderSql, connection, (SqlTransaction)transaction);
                cmd.Parameters.AddWithValue("@KlantId", order.Klant?.Id ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@OrderDatum", order.BestelDatum == DateTime.MinValue ? DateTime.UtcNow : order.BestelDatum);
                cmd.Parameters.AddWithValue("@OrderStatus", order.BestelStatus ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Id", order.Id);

                await cmd.ExecuteNonQueryAsync();

                const string deleteLines = @"DELETE FROM Bestelregel WHERE bestelling_id = @Id";

                await using var delCmd = new SqlCommand(deleteLines, connection, (SqlTransaction)transaction);
                delCmd.Parameters.AddWithValue("@Id", order.Id);
                await delCmd.ExecuteNonQueryAsync();

                if (order.Bestelregels != null && order.Bestelregels.Count > 0)
                {
                    foreach (var regel in order.Bestelregels)
                    {
                        const string insertLine = @"
INSERT INTO Bestelregel (bestelling_id, product_id, aantal)
VALUES (@OrderId, @ProductId, @Aantal);";

                        await using var lineCmd = new SqlCommand(insertLine, connection, (SqlTransaction)transaction);
                        lineCmd.Parameters.AddWithValue("@OrderId", order.Id);
                        lineCmd.Parameters.AddWithValue("@ProductId", regel.ProductId);
                        lineCmd.Parameters.AddWithValue("@Aantal", regel.Aantal <= 0 ? 1 : regel.Aantal);

                        await lineCmd.ExecuteNonQueryAsync();
                    }
                }
                else if (order.Producten != null && order.Producten.Count > 0)
                {
                    foreach (var product in order.Producten)
                    {
                        const string insertLine = @"
INSERT INTO Bestelregel (bestelling_id, product_id, aantal)
VALUES (@OrderId, @ProductId, @Aantal);";

                        await using var lineCmd = new SqlCommand(insertLine, connection, (SqlTransaction)transaction);
                        lineCmd.Parameters.AddWithValue("@OrderId", order.Id);
                        lineCmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                        lineCmd.Parameters.AddWithValue("@Aantal", 1);

                        await lineCmd.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                const string deleteLines = @"DELETE FROM Bestelregel WHERE bestelling_id = @Id";

                await using var cmdLines = new SqlCommand(deleteLines, connection, (SqlTransaction)transaction);
                cmdLines.Parameters.AddWithValue("@Id", id);
                await cmdLines.ExecuteNonQueryAsync();

                const string deleteOrder = @"DELETE FROM Bestelling WHERE bestelling_id = @Id";

                await using var cmdOrder = new SqlCommand(deleteOrder, connection, (SqlTransaction)transaction);
                cmdOrder.Parameters.AddWithValue("@Id", id);
                await cmdOrder.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #region Safe readers

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

        #endregion
    }
}