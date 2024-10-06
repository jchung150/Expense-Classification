using System;

namespace expense_classification.Models;

public class Bucket
{
    public int Id { get; set; }
    public string? Name { get; set; } // e.g., "Entertainment"
    public string? Vendor { get; set; } // e.g., "ST JAMES RESTAURANT"
}
