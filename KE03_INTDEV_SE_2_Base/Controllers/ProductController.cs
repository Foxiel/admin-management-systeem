using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KE03_INTDEV_SE_2_Base.Controllers;

public class ProductController : Controller
{
    private readonly ProductRepository _repository;

    public ProductController(ProductRepository repository)
    {
        _repository = repository;
    }

    // GET: Product
    // removed locatieId; added leverancierNaam filter (string)
    public async Task<IActionResult> Index(
        string? ean,
        string? naam,
        string? leverancierNaam,
        decimal? minPrijs,
        decimal? maxPrijs,
        int? huidigeVoorraadMax,
        string? status)
    {
        var products = (await _repository.GetFilteredAsync(ean, naam, leverancierNaam, minPrijs, maxPrijs, huidigeVoorraadMax, status)).ToList();

        var items = products.Select(p => new ProductListItem
        {
            ProductId = p.ProductId,
            EAN = p.EAN,
            Naam = p.Naam,
            Prijs = p.Prijs,
            HuidigeVoorraad = p.HuidigeVoorraad,
            Status = p.Status,
            LeverancierNaam = p.Leverancier?.Naam ?? string.Empty,
            LocatieNaam = p.Locatie?.Naam ?? string.Empty
        }).ToList();

        // For the index filter dropdown we use supplier names as values (not ids)
        var leveranciers = (await _repository.GetLeveranciersAsync())
            .Select(l => new SelectListItem(l.Naam, l.Naam)).ToList();

        // no locatie filter needed anymore
        ViewBag.Leveranciers = leveranciers;

        return View(items);
    }

    // GET: Product/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var product = await _repository.GetByIdAsync(id.Value);
        if (product == null) return NotFound();

        var model = MapToEditModel(product);
        model.LeverancierNaam = product.Leverancier?.Naam ?? string.Empty;
        model.LocatieNaam = product.Locatie?.Naam ?? string.Empty;

        return View(model);
    }

    // GET: Product/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Leveranciers = (await _repository.GetLeveranciersAsync())
            .Select(l => new SelectListItem(l.Naam, l.LeverancierId.ToString())).ToList();
        ViewBag.Locaties = (await _repository.GetLocatiesAsync())
            .Select(l => new SelectListItem(l.Naam, l.LocatieId.ToString())).ToList();
        return View();
    }

    // POST: Product/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("EAN,LeverancierId,LocatieId,Naam,Beschrijving,Prijs,Gewicht,Garantie,HuidigeVoorraad,MinimumVoorraad,Status,LeverancierNaam,LocatieGang,LocatieSchap,LocatieVak")] ProductEditModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Leveranciers = new List<SelectListItem>(); // geen dropdown maar behouden voor compat
            ViewBag.Locaties = new List<SelectListItem>();
            return View(model);
        }

        // Resolve leverancier by name (create when not exists)
        var leverancierId = 0;
        if (!string.IsNullOrWhiteSpace(model.LeverancierNaam))
            leverancierId = await _repository.GetOrCreateLeverancierByNameAsync(model.LeverancierNaam);

        // Resolve locatie by gang/schap/vak (create when not exists)
        var locatieId = 0;
        if (!string.IsNullOrWhiteSpace(model.LocatieGang) || !string.IsNullOrWhiteSpace(model.LocatieSchap) || !string.IsNullOrWhiteSpace(model.LocatieVak))
            locatieId = await _repository.GetOrCreateLocatieByGangSchapVakAsync(model.LocatieGang, model.LocatieSchap, model.LocatieVak);

        var product = MapToProduct(model);
        product.LeverancierId = leverancierId;
        product.LocatieId = locatieId;

        var newId = await _repository.AddAsync(product);
        if (newId > 0) return RedirectToAction(nameof(Index));

        return View(model);
    }

    // GET: Product/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var product = await _repository.GetByIdAsync(id.Value);
        if (product == null) return NotFound();

        var model = MapToEditModel(product);
        ViewBag.Leveranciers = (await _repository.GetLeveranciersAsync())
            .Select(l => new SelectListItem(l.Naam, l.LeverancierId.ToString())).ToList();
        ViewBag.Locaties = (await _repository.GetLocatiesAsync())
            .Select(l => new SelectListItem(l.Naam, l.LocatieId.ToString())).ToList();
        return View(model);
    }

    // POST: Product/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ProductId,EAN,LeverancierId,LocatieId,Naam,Beschrijving,Prijs,Gewicht,Garantie,HuidigeVoorraad,MinimumVoorraad,Status,LeverancierNaam,LocatieGang,LocatieSchap,LocatieVak")] ProductEditModel model)
    {
        if (id != model.ProductId) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Leveranciers = new List<SelectListItem>();
            ViewBag.Locaties = new List<SelectListItem>();
            return View(model);
        }

        var leverancierId = model.LeverancierId;
        if (!string.IsNullOrWhiteSpace(model.LeverancierNaam))
            leverancierId = await _repository.GetOrCreateLeverancierByNameAsync(model.LeverancierNaam);

        var locatieId = model.LocatieId;
        if (!string.IsNullOrWhiteSpace(model.LocatieGang) || !string.IsNullOrWhiteSpace(model.LocatieSchap) || !string.IsNullOrWhiteSpace(model.LocatieVak))
            locatieId = await _repository.GetOrCreateLocatieByGangSchapVakAsync(model.LocatieGang, model.LocatieSchap, model.LocatieVak);

        var product = MapToProduct(model);
        product.LeverancierId = leverancierId;
        product.LocatieId = locatieId;

        await _repository.UpdateAsync(product);
        return RedirectToAction(nameof(Index));
    }

    // GET: Product/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var product = await _repository.GetByIdAsync(id.Value);
        if (product == null) return NotFound();

        var model = MapToEditModel(product);
        model.LeverancierNaam = product.Leverancier?.Naam ?? string.Empty;
        model.LocatieNaam = product.Locatie?.Naam ?? string.Empty;

        return View(model);
    }

    // POST: Product/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _repository.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    #region Mapping helpers

    private static Product MapToProduct(ProductEditModel m) => new Product
    {
        ProductId = m.ProductId,
        EAN = m.EAN ?? string.Empty,
        LeverancierId = m.LeverancierId,
        LocatieId = m.LocatieId,
        Naam = m.Naam ?? string.Empty,
        Beschrijving = m.Beschrijving ?? string.Empty,
        Prijs = m.Prijs,
        Gewicht = m.Gewicht,
        Garantie = m.Garantie ?? string.Empty,
        HuidigeVoorraad = m.HuidigeVoorraad,
        MinimumVoorraad = m.MinimumVoorraad,
        Status = m.Status ?? string.Empty
    };

    private static ProductEditModel MapToEditModel(Product d) => new ProductEditModel
    {
        ProductId = d.ProductId,
        EAN = d.EAN,
        LeverancierId = d.LeverancierId,
        LocatieId = d.LocatieId,
        Naam = d.Naam,
        Beschrijving = d.Beschrijving,
        Prijs = d.Prijs,
        Gewicht = d.Gewicht,
        Garantie = d.Garantie,
        HuidigeVoorraad = d.HuidigeVoorraad,
        MinimumVoorraad = d.MinimumVoorraad,
        Status = d.Status
    };

    #endregion

    #region ViewModels (kept for views compatibility)

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

        // keep id fields for compatibility
        public int LeverancierId { get; set; }
        public int LocatieId { get; set; }

        // free text inputs (new)
        public string LeverancierNaam { get; set; } = string.Empty;
        public string LocatieGang { get; set; } = string.Empty;
        public string LocatieSchap { get; set; } = string.Empty;
        public string LocatieVak { get; set; } = string.Empty;

        public string Naam { get; set; } = string.Empty;
        public string Beschrijving { get; set; } = string.Empty;
        public decimal Prijs { get; set; }
        public decimal Gewicht { get; set; }
        public string Garantie { get; set; } = string.Empty;
        public int HuidigeVoorraad { get; set; }
        public int MinimumVoorraad { get; set; }
        public string Status { get; set; } = string.Empty;

        // display fields
        public string LeverancierNaamDisplay { get; set; } = string.Empty;
        public string LocatieNaam { get; set; } = string.Empty;
    }

    #endregion
}