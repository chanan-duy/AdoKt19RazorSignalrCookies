using AdoKt19RazorSignalrCookies.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AdoKt19RazorSignalrCookies.Pages;

[Authorize]
public class ChatModel : PageModel
{
	private readonly AppDbContext _dbContext;

	public ChatModel(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public string CurrentUsername { get; private set; } = string.Empty;
	public List<ChatMessageViewModel> Messages { get; private set; } = [];

	public async Task OnGetAsync()
	{
		CurrentUsername = User.Identity?.Name ?? "unknown";

		Messages = await _dbContext.ChatMessages
			.AsNoTracking()
			.Include(message => message.User)
			.OrderByDescending(message => message.CreatedAtUtc)
			.Take(50)
			.Select(message => new ChatMessageViewModel
			{
				Username = message.User.Username,
				Text = message.Text,
				CreatedAtLocal = message.CreatedAtUtc.ToLocalTime(),
			})
			.ToListAsync();

		Messages.Reverse();
	}

	public class ChatMessageViewModel
	{
		public string Username { get; set; } = string.Empty;
		public string Text { get; set; } = string.Empty;
		public DateTime CreatedAtLocal { get; set; }
	}
}
