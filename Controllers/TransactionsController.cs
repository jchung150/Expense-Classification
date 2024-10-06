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
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        [HttpGet]
        public async Task<IActionResult> GetVendorsByBucket(string bucketName)
        {
            // Fetch the list of vendors for the selected bucket
            var vendors = await _context.Buckets
                                        .Where(b => b.Name == bucketName)
                                        .Select(b => b.Vendor)
                                        .Distinct()
                                        .ToListAsync();

            // Return the list of vendors as JSON
            return Json(vendors);
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
                    HasHeaderRecord = false,
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

                        // Set the UserId for the transaction
                        transaction.UserId = user.Id;

                        // Check if the Bucket already exists
                        var existingBucket = await _context.Buckets
                                                        .FirstOrDefaultAsync(b => b.Name == transaction.BucketName && b.Vendor == transaction.Vendor);
                        
                        // If the bucket doesn't exist, add a new entry to the Bucket table
                        if (existingBucket == null)
                        {
                            var newBucket = new Bucket
                            {
                                Name = transaction.BucketName,
                                Vendor = transaction.Vendor
                            };
                            _context.Buckets.Add(newBucket);
                        }

                        // Add the transaction to the Transactions table
                        _context.Transactions.Add(transaction);
                    }
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Transactions and Buckets saved to the database.");
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

        //List transaction
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Fetch transactions for the logged-in user
            var transactions = await _context.Transactions
                                            .Where(t => t.UserId == user.Id)
                                            .ToListAsync();

            return View(transactions);  
        }

        //Edit transaction
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var transaction = await _context.Transactions
                                            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);

            if (user == null) 
            {
                return Unauthorized();
            }
            if (transaction == null)
            {
                return NotFound();
            }


            // Fetch unique bucket names from the database
            var uniqueBuckets = await _context.Buckets
                                            .Select(b => b.Name)
                                            .Distinct()
                                            .ToListAsync();

            // Populate the ViewBag with the unique bucket names
            ViewBag.BucketList = uniqueBuckets.Select(b => new SelectListItem
            {
                Value = b,
                Text = b
            }).ToList();

            // Populate the VendorList based on the selected bucket
            var vendors = await _context.Buckets
                                        .Where(b => b.Name == transaction.BucketName)
                                        .Select(b => b.Vendor)
                                        .Distinct()
                                        .ToListAsync();

            ViewBag.VendorList = vendors.Select(v => new SelectListItem
            {
                Value = v,
                Text = v
            }).ToList();

            return View(transaction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Transaction transaction)
        {
            var user = await _userManager.GetUserAsync(User);
            
            // Manually set the UserId for the transaction before validation
            if (user != null)
            {
                transaction.UserId = user.Id;
            }

            if (user == null || transaction.UserId != user.Id)
            {
                return Unauthorized();
            }

            _context.Update(transaction);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Transaction updated successfully.";
            return RedirectToAction("List");

            // If ModelState is invalid, repopulate dropdowns
            var uniqueBuckets = await _context.Buckets
                                            .Select(b => b.Name)
                                            .Distinct()
                                            .ToListAsync();

            ViewBag.BucketList = uniqueBuckets.Select(b => new SelectListItem
            {
                Value = b,
                Text = b
            }).ToList();

            var vendors = await _context.Buckets
                                        .Where(b => b.Name == transaction.BucketName)
                                        .Select(b => b.Vendor)
                                        .Distinct()
                                        .ToListAsync();

            ViewBag.VendorList = vendors.Select(v => new SelectListItem
            {
                Value = v,
                Text = v
            }).ToList();

            // Return the view with the validation errors
            return View(transaction);
        }

        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.Id == id);
        }

        //Delete transaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var transaction = await _context.Transactions
                                            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);

            if (transaction == null)
            {
                return NotFound();
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Transaction deleted successfully.";
            return RedirectToAction("List");
        }

        // GET: Add Transaction
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            // Fetch unique bucket names from the database
            var uniqueBuckets = await _context.Buckets
                                            .Select(b => b.Name)
                                            .Distinct()
                                            .OrderBy(b => b)
                                            .ToListAsync();

            // Populate the ViewBag with the unique bucket names
            ViewBag.BucketList = uniqueBuckets.Select(b => new SelectListItem
            {
                Value = b,
                Text = b
            }).ToList();

            return View();
        }

        // POST: Add Transaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Transaction transaction)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Ensure the BucketName is not null
            if (string.IsNullOrWhiteSpace(transaction.BucketName))
            {
                ModelState.AddModelError("BucketName", "Bucket is required.");
                return View(transaction);
            }

            // Set the UserId for the transaction
            transaction.UserId = user.Id;
            
            // Add the transaction to the database
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transaction added successfully.";
            return RedirectToAction("List");
        }
    }
}
