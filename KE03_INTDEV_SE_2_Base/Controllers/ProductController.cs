using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KE03_INTDEV_SE_2_Base.Controllers;

public class ProductController : Controller
{
    private readonly IConfiguration _configuration;

    public ProductController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetConnectionString() => _configuration.GetConnectionString("Connection")!;

    // GET: Product
    public async Task<IActionResult> Index(
        string? ean,
        string? naam,
        int? leverancierId,
        int? locatieId,
        decimal? minPrijs,
        decimal? maxPrijs,
        int? huidigeVoorraadMax,
        string? status)
    {
        var items = new List<ProductListItem>();

        var sql = @"
SELECT p.product_id, p.ean, p.naam, p.prijs, p.huidige_voorraad, p.status,
       l.naam AS leveranciernaam, loc.naam AS locatienaam
FROM product p
LEFT JOIN leverancier l ON p.leverancier_id = l.leverancier_id
LEFT JOIN locatie loc ON p.locatie_id = loc.locatie_id
WHERE 1=1
";
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

        if (leverancierId.HasValue && leverancierId.Value != 0)
        {
            sql += " AND p.leverancier_id = @leverancierId";
            parameters.Add(new SqlParameter("@leverancierId", leverancierId.Value));
        }

        if (locatieId.HasValue && locatieId.Value != 0)
        {
            sql += " AND p.locatie_id = @locatieId";
            parameters.Add(new SqlParameter("@locatieId", locatieId.Value));
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

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();

        await using (var command = new SqlCommand(sql, connection))
        {
            if (parameters.Count > 0) command.Parameters.AddRange(parameters.ToArray());
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new ProductListItem
                {
                    ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
                    EAN = reader.IsDBNull(reader.GetOrdinal("ean")) ? string.Empty : reader.GetString(reader.GetOrdinal("ean")),
                    Naam = reader.IsDBNull(reader.GetOrdinal("naam")) ? string.Empty : reader.GetString(reader.GetOrdinal("naam")),
                    Prijs = reader.IsDBNull(reader.GetOrdinal("prijs")) ? 0m : reader.GetDecimal(reader.GetOrdinal("prijs")),
                    HuidigeVoorraad = reader.IsDBNull(reader.GetOrdinal("huidige_voorraad")) ? 0 : reader.GetInt32(reader.GetOrdinal("huidige_voorraad")),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? string.Empty : reader.GetString(reader.GetOrdinal("status")),
                    LeverancierNaam = reader.IsDBNull(reader.GetOrdinal("leveranciernaam")) ? string.Empty : reader.GetString(reader.GetOrdinal("leveranciernaam")),
                    LocatieNaam = reader.IsDBNull(reader.GetOrdinal("locatienaam")) ? string.Empty : reader.GetString(reader.GetOrdinal("locatienaam"))
                });
            }
        }

        ViewBag.Leveranciers = await GetLeveranciersSelectList();
        ViewBag.Locaties = await GetLocatiesSelectList();

        return View(items);
    }

    // GET: Product/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var model = await GetProductEditModel(id.Value);
        if (model == null) return NotFound();

        model.LeverancierNaam = await GetLeverancierNaam(model.LeverancierId);
        model.LocatieNaam = await GetLocatieNaam(model.LocatieId);

        return View(model);
    }

    // GET: Product/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Leveranciers = await GetLeveranciersSelectList();
        ViewBag.Locaties = await GetLocatiesSelectList();
        return View();
    }

    // POST: Product/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("EAN,LeverancierId,LocatieId,Naam,Beschrijving,Prijs,Gewicht,Garantie,HuidigeVoorraad,MinimumVoorraad,Status")] ProductEditModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Leveranciers = await GetLeveranciersSelectList();
            ViewBag.Locaties = await GetLocatiesSelectList();
            return View(model);
        }

        var sql = @"
INSERT INTO product (ean, leverancier_id, locatie_id, naam, beschrijving, prijs, gewicht, garantie, huidige_voorraad, minimum_voorraad, status)
VALUES (@ean, @leverancierId, @locatieId, @naam, @beschrijving, @prijs, @gewicht, @garantie, @huidige_voorraad, @minimum_voorraad, @status);
SELECT CAST(SCOPE_IDENTITY() AS int);
";
        await using var connection = new SqlConnection(GetConnectionString());
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
        if (result != null && int.TryParse(result.ToString(), out _))
        {
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Leveranciers = await GetLeveranciersSelectList();
        ViewBag.Locaties = await GetLocatiesSelectList();
        return View(model);
    }

    // GET: Product/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var model = await GetProductEditModel(id.Value);
        if (model == null) return NotFound();

        ViewBag.Leveranciers = await GetLeveranciersSelectList();
        ViewBag.Locaties = await GetLocatiesSelectList();
        return View(model);
    }

    // POST: Product/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ProductId,EAN,LeverancierId,LocatieId,Naam,Beschrijving,Prijs,Gewicht,Garantie,HuidigeVoorraad,MinimumVoorraad,Status")] ProductEditModel model)
    {
        if (id != model.ProductId) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Leveranciers = await GetLeveranciersSelectList();
            ViewBag.Locaties = await GetLocatiesSelectList();
            return View(model);
        }

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
        await using var connection = new SqlConnection(GetConnectionString());
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

        return RedirectToAction(nameof(Index));
    }

    // GET: Product/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var model = await GetProductEditModel(id.Value);
        if (model == null) return NotFound();

        model.LeverancierNaam = await GetLeverancierNaam(model.LeverancierId);
        model.LocatieNaam = await GetLocatieNaam(model.LocatieId);

        return View(model);
    }

    // POST: Product/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sql = "DELETE FROM product WHERE product_id = @id";
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
        return RedirectToAction(nameof(Index));
    }

    #region Helpers

    private async Task<ProductEditModel?> GetProductEditModel(int id)
    {
        var sql = @"
SELECT product_id, ean, leverancier_id, locatie_id, naam, beschrijving, prijs, gewicht, garantie, huidige_voorraad, minimum_voorraad, status
FROM product
WHERE product_id = @id";
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ProductEditModel
            {
                ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
                EAN = reader.IsDBNull(reader.GetOrdinal("ean")) ? string.Empty : reader.GetString(reader.GetOrdinal("ean")),
                LeverancierId = reader.IsDBNull(reader.GetOrdinal("leverancier_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("leverancier_id")),
                LocatieId = reader.IsDBNull(reader.GetOrdinal("locatie_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("locatie_id")),
                Naam = reader.IsDBNull(reader.GetOrdinal("naam")) ? string.Empty : reader.GetString(reader.GetOrdinal("naam")),
                Beschrijving = reader.IsDBNull(reader.GetOrdinal("beschrijving")) ? string.Empty : reader.GetString(reader.GetOrdinal("beschrijving")),
                Prijs = reader.IsDBNull(reader.GetOrdinal("prijs")) ? 0m : reader.GetDecimal(reader.GetOrdinal("prijs")),
                Gewicht = reader.IsDBNull(reader.GetOrdinal("gewicht")) ? 0m : reader.GetDecimal(reader.GetOrdinal("gewicht")),
                Garantie = reader.IsDBNull(reader.GetOrdinal("garantie")) ? string.Empty : reader.GetString(reader.GetOrdinal("garantie")),
                HuidigeVoorraad = reader.IsDBNull(reader.GetOrdinal("huidige_voorraad")) ? 0 : reader.GetInt32(reader.GetOrdinal("huidige_voorraad")),
                MinimumVoorraad = reader.IsDBNull(reader.GetOrdinal("minimum_voorraad")) ? 0 : reader.GetInt32(reader.GetOrdinal("minimum_voorraad")),
                Status = reader.IsDBNull(reader.GetOrdinal("status")) ? string.Empty : reader.GetString(reader.GetOrdinal("status"))
            };
        }

        return null;
    }

    private async Task<List<SelectListItem>> GetLeveranciersSelectList()
    {
        var list = new List<SelectListItem> { new SelectListItem("— Geen —", "0") };
        var sql = "SELECT leverancier_id, naam FROM leverancier ORDER BY naam";

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new SelectListItem
            {
                Text = reader.IsDBNull(reader.GetOrdinal("naam")) ? string.Empty : reader.GetString(reader.GetOrdinal("naam")),
                Value = reader.GetInt32(reader.GetOrdinal("leverancier_id")).ToString()
            });
        }

        return list;
    }

    private async Task<List<SelectListItem>> GetLocatiesSelectList()
    {
        var list = new List<SelectListItem> { new SelectListItem("— Geen —", "0") };
        var sql = "SELECT locatie_id, naam FROM locatie ORDER BY naam";

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new SelectListItem
            {
                Text = reader.IsDBNull(reader.GetOrdinal("naam")) ? string.Empty : reader.GetString(reader.GetOrdinal("naam")),
                Value = reader.GetInt32(reader.GetOrdinal("locatie_id")).ToString()
            });
        }

        return list;
    }

    private async Task<string> GetLeverancierNaam(int leverancierId)
    {
        if (leverancierId == 0) return string.Empty;
        var sql = "SELECT naam FROM leverancier WHERE leverancier_id = @id";
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", leverancierId);
        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? string.Empty;
    }

    private async Task<string> GetLocatieNaam(int locatieId)
    {
        if (locatieId == 0) return string.Empty;
        var sql = "SELECT naam FROM locatie WHERE locatie_id = @id";
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", locatieId);
        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? string.Empty;
    }

    #endregion

    #region ViewModels

    public class ProductListItem
    {
        public int ProductId { get; set; }
        public string EAN { get; set; } = string.Empty;
        public string Naam { get; set; } = string.Empty;
        public decimal Prijs { get; set; }
        public int HuidigeVoorraad { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LeverancierNaam { get; set; } = string.Empty;
        public string LocatieNaam { get; set; } = string.Empty;
    }

    public class ProductEditModel
    {
        public int ProductId { get; set; }
        public string EAN { get; set; } = string.Empty;
        public int LeverancierId { get; set; }
        public int LocatieId { get; set; }
        public string Naam { get; set; } = string.Empty;
        public string Beschrijving { get; set; } = string.Empty;
        public decimal Prijs { get; set; }
        public decimal Gewicht { get; set; }
        public string Garantie { get; set; } = string.Empty;
        public int HuidigeVoorraad { get; set; }
        public int MinimumVoorraad { get; set; }
        public string Status { get; set; } = string.Empty;

        // display fields
        public string LeverancierNaam { get; set; } = string.Empty;
        public string LocatieNaam { get; set; } = string.Empty;
    }

    #endregion
}