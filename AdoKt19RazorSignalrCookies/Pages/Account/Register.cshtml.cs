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

public class RegisterModel : PageModel
{
	private readonly AppDbContext _dbContext;
	private readonly IPasswordHasher<UserEntity> _passwordHasher;

	public RegisterModel(AppDbContext dbContext, IPasswordHasher<UserEntity> passwordHasher)
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

		var username = Input.Username.Trim();
		var usernameNormalized = username.ToUpperInvariant();

		var userExists = await _dbContext.Users
			.AnyAsync(user => user.UsernameNormalized == usernameNormalized);
		if (userExists)
		{
			ModelState.AddModelError(string.Empty, "user already exists");
			return Page();
		}

		var user = new UserEntity
		{
			Username = username,
			UsernameNormalized = usernameNormalized,
			RegisteredAtUtc = DateTime.UtcNow,
		};
		user.PasswordHash = _passwordHasher.HashPassword(user, Input.Password);

		_dbContext.Users.Add(user);
		await _dbContext.SaveChangesAsync();

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
