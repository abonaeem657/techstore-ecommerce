using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace techstore.Models;
public class Users
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    [RegularExpression(@"^[^;\r\n]+$", ErrorMessage = "Names cannot contain semicolons or line breaks.")]
    public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)]
    [RegularExpression(@"^[^;\r\n]+$", ErrorMessage = "Email cannot contain semicolons or line breaks.")]
    public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "Use a password between 12 and 128 characters.")]
    public string Password { get; set; } = string.Empty;
    [BindNever, ValidateNever]
    public string PasswordHash { get; set; } = string.Empty;
}

