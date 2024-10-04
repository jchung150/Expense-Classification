using expense_classification.Data;
using expense_classification.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace expense_classification.Controllers
{
    [Authorize(Roles = "admin")]
    public class BucketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BucketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Buckets/List
        public async Task<IActionResult> List()
        {
            var buckets = await _context.Buckets
                                .OrderBy(b => b.Name)  
                                .ToListAsync();

            return View(buckets);
        }

        // GET: Buckets/Add
        public IActionResult Add()
        {
            return View();
        }

        // POST: Buckets/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Bucket bucket)
        {
            if (ModelState.IsValid)
            {
                _context.Buckets.Add(bucket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(List));
            }
            return View(bucket);
        }

        // GET: Buckets/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var bucket = await _context.Buckets.FindAsync(id);
            if (bucket == null)
            {
                return NotFound();
            }
            return View(bucket);
        }

        // POST: Buckets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bucket bucket)
        {
            if (id != bucket.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(bucket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(List));
            }
            return View(bucket);
        }

        // POST: Buckets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bucket = await _context.Buckets.FindAsync(id);
            _context.Buckets.Remove(bucket);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(List));
        }
    }
}