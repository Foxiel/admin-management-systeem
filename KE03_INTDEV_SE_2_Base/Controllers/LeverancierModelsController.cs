
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KE03_INTDEV_SE_2_Base.Models;

public class LeverancierModelsController : Controller
{
    private readonly appDbContext _context;

    public LeverancierModelsController(appDbContext context)
    {
        _context = context;
    }

    // GET: LEVERANCIERMODELS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.LeverancierModel.ToListAsync());
    }

    // GET: LEVERANCIERMODELS/Details/5
    public async Task<IActionResult> Details(int? leverancier_id)
    {
        if (leverancier_id == null)
        {
            return NotFound();
        }

        var leveranciermodel = await _context.LeverancierModel
            .FirstOrDefaultAsync(m => m.Leverancier_id == leverancier_id);
        if (leveranciermodel == null)
        {
            return NotFound();
        }

        return View(leveranciermodel);
    }

    // GET: LEVERANCIERMODELS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: LEVERANCIERMODELS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Leverancier_id,Leverancier_naam")] LeverancierModel leveranciermodel)
    {
        if (ModelState.IsValid)
        {
            _context.Add(leveranciermodel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(leveranciermodel);
    }

    // GET: LEVERANCIERMODELS/Edit/5
    public async Task<IActionResult> Edit(int? leverancier_id)
    {
        if (leverancier_id == null)
        {
            return NotFound();
        }

        var leveranciermodel = await _context.LeverancierModel.FindAsync(leverancier_id);
        if (leveranciermodel == null)
        {
            return NotFound();
        }
        return View(leveranciermodel);
    }

    // POST: LEVERANCIERMODELS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? leverancier_id, [Bind("Leverancier_id,Leverancier_naam")] LeverancierModel leveranciermodel)
    {
        if (leverancier_id != leveranciermodel.Leverancier_id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(leveranciermodel);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LeverancierModelExists(leveranciermodel.Leverancier_id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(leveranciermodel);
    }

    // GET: LEVERANCIERMODELS/Delete/5
    public async Task<IActionResult> Delete(int? leverancier_id)
    {
        if (leverancier_id == null)
        {
            return NotFound();
        }

        var leveranciermodel = await _context.LeverancierModel
            .FirstOrDefaultAsync(m => m.Leverancier_id == leverancier_id);
        if (leveranciermodel == null)
        {
            return NotFound();
        }

        return View(leveranciermodel);
    }

    // POST: LEVERANCIERMODELS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? leverancier_id)
    {
        var leveranciermodel = await _context.LeverancierModel.FindAsync(leverancier_id);
        if (leveranciermodel != null)
        {
            _context.LeverancierModel.Remove(leveranciermodel);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool LeverancierModelExists(int? leverancier_id)
    {
        return _context.LeverancierModel.Any(e => e.Leverancier_id == leverancier_id);
    }
}
