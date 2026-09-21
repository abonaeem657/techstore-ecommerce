using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using techstore.Services;
using techstore.Models;

namespace techstore.Controllers;

[AutoValidateAntiforgeryToken]
public class StoreController : Controller
{
    // Keep the original in-memory catalog. A lock protects concurrent requests.
    private static readonly List<Product> Products = new();
    private static readonly object ProductLock = new();

    private static int _nextProductId;
    private readonly UserFileStore _accounts;
    private readonly ILogger<StoreController> _logger;

    public StoreController(UserFileStore accounts, ILogger<StoreController> logger)
    {
        _accounts = accounts;
        _logger = logger;
    }
    public IActionResult StartP() => View();

    public IActionResult Viewdata(string? search, string? category)
    {
        lock (ProductLock)
        {
            ViewData["Search"] = search?.Trim();
            ViewData["Category"] = category;
            ViewData["Categories"] = Products.Select(p => p.Type).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList();
            ViewData["TotalCount"] = Products.Count;
            var results = Products.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
                results = results.Where(p => p.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(category))
                results = results.Where(p => string.Equals(p.Type, category, StringComparison.OrdinalIgnoreCase));
            return View(results.Select(CopyProduct).ToList());
        }
    }

    public IActionResult one(int id) => ProductView(id, "one");
    [Authorize(AuthenticationSchemes = "StoreCookie")]
    public IActionResult edit(int id) => ProductView(id, "edit");
    [HttpGet]
    [Authorize(AuthenticationSchemes = "StoreCookie")]
    public IActionResult delete(int id) => ProductView(id, "delete");
    [Authorize(AuthenticationSchemes = "StoreCookie")]
    public IActionResult additem() => View(new Product());

    [HttpPost]
    [Authorize(AuthenticationSchemes = "StoreCookie")]
    public IActionResult additem(Product product)
    {
        if (!ModelState.IsValid) return View(product);
        lock (ProductLock)
        {
            product.Id = ++_nextProductId;
            product.Name = product.Name.Trim();
            product.Type = product.Type.Trim();
            Products.Add(product);
        }
        TempData["Success"] = "Product added successfully.";
        return RedirectToAction(nameof(Viewdata));
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "StoreCookie")]
    public IActionResult edit(Product product)
    {
        lock (ProductLock)
        {
            var existing = Products.FirstOrDefault(p => p.Id == product.Id);
            if (existing == null) return MissingProduct();
            if (!ModelState.IsValid) return View(product);
            existing.Name = product.Name.Trim();
            existing.Type = product.Type.Trim();
            existing.Price = product.Price;
        }
        TempData["Success"] = "Product updated successfully.";
        return RedirectToAction(nameof(Viewdata));
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(AuthenticationSchemes = "StoreCookie")]
    public IActionResult DeleteConfirmed(int id)
    {
        lock (ProductLock)
        {
            var product = Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return MissingProduct();
            Products.Remove(product);
        }
        TempData["Success"] = "Product deleted successfully.";
        return RedirectToAction(nameof(Viewdata));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(Login login, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        if (ModelState.IsValid)
        {
            try
            {
                var user = _accounts.Verify(login.Email, login.Password);
                if (user != null)
                {
                    var identity = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Name),
                        new Claim(ClaimTypes.Email, user.Email)
                    }, "StoreCookie");
                    await HttpContext.SignInAsync("StoreCookie", new ClaimsPrincipal(identity),
                        new AuthenticationProperties { IsPersistent = false });
                    return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Viewdata));
                }
                ModelState.AddModelError("", "The email or password is incorrect.");
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                _logger.LogError("Unable to access the account file. Error type: {ErrorType}", ex.GetType().Name);
                ModelState.AddModelError("", "Login is temporarily unavailable. Please try again later.");
            }
        }
        login.Password = string.Empty;
        ClearPasswordInput();
        return View(login);
    }

    [HttpPost, Authorize(AuthenticationSchemes = "StoreCookie")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("StoreCookie");
        return RedirectToAction(nameof(StartP));
    }

    [HttpGet]
    public IActionResult SignUp() => View();
    [HttpPost]
    public IActionResult SignUp(Users user)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (_accounts.Create(user))
                {
                    TempData["Success"] = "Account created. You can now log in.";
                    return RedirectToAction(nameof(Login));
                }
                ModelState.AddModelError("", "An account with this email already exists.");
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                _logger.LogError("Unable to save the account file. Error type: {ErrorType}", ex.GetType().Name);
                ModelState.AddModelError("", "Your account could not be saved. Please try again later.");
            }
        }
        user.Password = string.Empty;
        user.PasswordHash = string.Empty;
        ClearPasswordInput();
        return View(user);
    }

    private void ClearPasswordInput()
    {
        // Preserve validation errors without returning submitted passwords in view state.
        var errors = ModelState["Password"]?.Errors.Select(e => e.ErrorMessage).ToList();
        ModelState.Remove("Password");
        if (errors != null)
            foreach (var error in errors) ModelState.AddModelError("Password", error);
    }

    private IActionResult ProductView(int id, string view)
    {
        lock (ProductLock)
        {
            var product = Products.FirstOrDefault(p => p.Id == id);
            return product == null ? MissingProduct() : View(view, CopyProduct(product));
        }
    }

    private IActionResult MissingProduct()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("ProductNotFound");
    }

    private static Product CopyProduct(Product product) =>
        new() { Id = product.Id, Name = product.Name, Type = product.Type, Price = product.Price };
}


