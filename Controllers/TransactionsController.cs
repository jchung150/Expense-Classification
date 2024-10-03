using expense_classification.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using CsvHelper;
using Microsoft.AspNetCore.Hosting;
using NuGet.Protocol.Core.Types;
using System.Globalization;
using CsvHelper.Configuration;
using expense_classification.Data;

namespace expense_classification.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, ILogger<TransactionsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        // GET: Transactions/Upload
        public IActionResult Upload()
        {
            return View();
        }

    // POST: Transactions/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Please select a file to upload.");
            return View();
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View();
            }

            _logger.LogInformation("User {UserId} is uploading a file.", user.Id);

            // Define the path to save the file
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads", user.Id);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Get the original file name
            var originalFileName = Path.GetFileName(file.FileName);

            // Define the file path
            var filePath = Path.Combine(uploadsFolder, originalFileName);

            // Save the uploaded file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Rename the file by adding '.imported' extension
            var importedFilePath = filePath + ".imported";
            System.IO.File.Move(filePath, importedFilePath);

            // Process the renamed file
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,  // No header
                MissingFieldFound = null,
                BadDataFound = null,
                IgnoreBlankLines = true
            };

            using (var reader = new StreamReader(importedFilePath))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Context.RegisterClassMap<TransactionMap>();
                var records = csv.GetRecords<Transaction>();

                foreach (var transaction in records)
                {
                    _logger.LogInformation("Adding transaction: {Vendor}, {Amount}, {Date}", transaction.Vendor, transaction.Amount, transaction.Date);
                    transaction.UserId = user.Id;
                    _context.Transactions.Add(transaction);
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Transactions saved to the database.");
            }

            TempData["SuccessMessage"] = "Transactions uploaded successfully.";
            return RedirectToAction("Upload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the file.");
            ModelState.AddModelError("", $"An error occurred while processing the file: {ex.Message}");
            return View();
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAllTransactions()
    {
        try
        {
            _logger.LogInformation("Admin is deleting all transactions.");

            // Delete all transactions
            _context.Transactions.RemoveRange(_context.Transactions);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "All transactions have been deleted.";
            return RedirectToAction(nameof(Upload));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting transactions.");
            TempData["ErrorMessage"] = "An error occurred while deleting transactions.";
            return RedirectToAction(nameof(Upload));
        }
    }

    public sealed class TransactionMap : ClassMap<Transaction>
    {
    public TransactionMap()
    {
        Map(m => m.Date)
            .Index(0)
            .TypeConverterOption.Format("MM/dd/yyyy");
        Map(m => m.Vendor)
            .Index(1);
        Map(m => m.BucketName)
            .Index(2);
        Map(m => m.Amount)
            .Index(3)
            .TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture);
        Map(m => m.Balance)
            .Index(4)
            .TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture);
    }
    }
}
}
