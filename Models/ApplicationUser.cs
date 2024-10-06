using System;
using Microsoft.AspNetCore.Identity;

namespace expense_classification.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Transaction>? Transactions { get; set; }

    public bool IsApproved { get; set; } = false;
}
