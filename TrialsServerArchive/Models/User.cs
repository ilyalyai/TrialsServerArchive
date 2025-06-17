// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class UserExtensions
{
    public static string GetInitials(this ApplicationUser user)
    {
        if (string.IsNullOrEmpty(user?.FullName))
            return "System";

        var parts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 1) return "System";

        var lastName = parts[0];
        var initials = string.Join(" ", parts.Skip(1).Select(p => p.Length > 0 ? p[0] + "." : ""));
        return $"{lastName} {initials}";
    }
}