using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AdoKt19RazorSignalrCookies.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AdoKt19RazorSignalrCookies.Pages.Account;

public class LoginModel : PageModel
{
	private readonly AppDbContext _dbContext;
	private readonly IPasswordHasher<UserEntity> _passwordHasher;

	public LoginModel(AppDbContext dbContext, IPasswordHasher<UserEntity> passwordHasher)
	{
		_dbContext = dbContext;
		_passwordHasher = passwordHasher;
	}

	[BindProperty]
	public InputModel Input { get; set; } = new();

	public IActionResult OnGet()
	{
		if (User.Identity?.IsAuthenticated == true)
		{
			return RedirectToPage("/Chat");
		}

		return Page();
	}

	public async Task<IActionResult> OnPostAsync()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		var usernameNormalized = Input.Username.Trim().ToUpperInvariant();
		var user = await _dbContext.Users
			.FirstOrDefaultAsync(entity => entity.UsernameNormalized == usernameNormalized);
		if (user is null)
		{
			ModelState.AddModelError(string.Empty, "invalid user or password");
			return Page();
		}

		var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Input.Password);
		if (verifyResult == PasswordVerificationResult.Failed)
		{
			ModelState.AddModelError(string.Empty, "invalid user or password");
			return Page();
		}

		await SignInAsync(user);
		return RedirectToPage("/Chat");
	}

	private async Task SignInAsync(UserEntity user)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.Name, user.Username),
		};

		var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
		var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

		await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
	}

	public class InputModel
	{
		[Required]
		[StringLength(32, MinimumLength = 3)]
		[Display(Name = "user")]
		public string Username { get; set; } = string.Empty;

		[Required]
		[StringLength(64, MinimumLength = 4)]
		[DataType(DataType.Password)]
		[Display(Name = "password")]
		public string Password { get; set; } = string.Empty;
	}
}
