
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KE03_INTDEV_SE_2_Base.Models;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;


public class OrdersController : Controller
{
    private readonly appDbContext _context;

    public OrdersController(appDbContext context)
    {
        _context = context;
    }

    // GET: ORDERS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Order.ToListAsync());
    }

    // GET: ORDERS/Details/5
    public async Task<IActionResult> Details(int? orderid)
    {
        if (orderid == null)
        {
            return NotFound();
        }

        var order = await _context.Order
            .FirstOrDefaultAsync(m => m.Id == orderid);
        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // GET: ORDERS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ORDERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("OrderId,OrderDatum,OrderStatus,BetaalStatus,Verzendkosten,TotaalBedrag,KlantId,ProductId,Aantal")] Order order)
    {
        if (ModelState.IsValid)
        {
            _context.Add(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(order);
    }

    // GET: ORDERS/Edit/5
    public async Task<IActionResult> Edit(int? orderid)
    {
        if (orderid == null)
        {
            return NotFound();
        }

        var order = await _context.Order.FindAsync(orderid);
        if (order == null)
        {
            return NotFound();
        }
        return View(order);
    }

    // POST: ORDERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? orderid, [Bind("OrderId,OrderDatum,OrderStatus,BetaalStatus,Verzendkosten,TotaalBedrag,KlantId,ProductId,Aantal")] Order order)
    {
        if (orderid != order.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(order);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(order.Id))
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
        return View(order);
    }

    // GET: ORDERS/Delete/5
    public async Task<IActionResult> Delete(int? orderid)
    {
        if (orderid == null)
        {
            return NotFound();
        }

        var order = await _context.Order
            .FirstOrDefaultAsync(m => m.Id == orderid);
        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // POST: ORDERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? orderid)
    {
        var order = await _context.Order.FindAsync(orderid);
        if (order != null)
        {
            _context.Order.Remove(order);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool OrderExists(int? orderid)
    {
        return _context.Order.Any(e => e.Id == orderid);
    }
}
