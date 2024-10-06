using System;

namespace expense_classification.Models;

public class Transaction
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string? Vendor { get; set; }
    public string? BucketName { get; set; }
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public string? UserId { get; set; } // Foreign key to ApplicationUser
    public ApplicationUser? User { get; set; }
}
