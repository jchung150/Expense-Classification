using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using expense_classification.Models;
using expense_classification.Data;

public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Reports
    public IActionResult Reports()
    {
        return View();
    }

    // POST: Generate Report
    [HttpPost]
    public async Task<IActionResult> GenerateReport(int year)
    {
        ViewBag.Year = year;

        var reportData = await _context.Transactions
            .Where(t => t.Date.Year == year)
            .ToListAsync();
        
        var groupedData = reportData
            .GroupBy(t => t.BucketName)
            .Select(g => new ReportViewModel
            {
                BucketName = g.Key,
                TotalAmount = g.Sum(t => t.Amount)  
            });
        return View("Reports", groupedData);
    }
}