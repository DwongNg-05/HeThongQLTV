using HeThongQLTV.Models;
using Microsoft.AspNetCore.Identity;

namespace HeThongQLTV.Data;

public static class AdminBootstrap
{
    // Only initialize an empty database; never reset existing credentials on restart.
    public static void Initialize(LibraryDb db, string? username, string? password)
    {
        if (db.Users.Any() || string.IsNullOrEmpty(password)) return;
        if (string.IsNullOrWhiteSpace(username) || password.Length < 12 || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("BootstrapAdmin requires a username and a password of at least 12 characters.");

        var user = new AppUser { Username = username.Trim(), FullName = "Quản trị viên", Role = "Admin" };
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
        db.Users.Add(user);
        db.SaveChanges();
    }
}
