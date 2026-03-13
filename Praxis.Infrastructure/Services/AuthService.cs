using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly PraxisDbContext _context;
    private readonly IPasswordService _passwordService;

    public AuthService(PraxisDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<User> RegisterUserAsync(string username, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Benutzername darf nicht leer sein.");

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Passwort darf nicht leer sein.");

        if (role != Roles.Administrator && role != Roles.Mitarbeiter)
            throw new ArgumentException("Ungültige Rolle.");

        var exists = await _context.Users.AnyAsync(u => u.Username == username);
        if (exists)
            throw new InvalidOperationException("Benutzername existiert bereits.");

        var user = new User
        {
            Username = username.Trim(),
            PasswordHash = _passwordService.HashPassword(password),
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        username = (username ?? "").Trim();
        password = (password ?? "").Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return null;

        return _passwordService.VerifyPassword(password, user.PasswordHash)
            ? user
            : null;
    }

    public async Task ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new InvalidOperationException("Benutzer wurde nicht gefunden.");

        var passwordMatches = _passwordService.VerifyPassword(oldPassword, user.PasswordHash);
        if (!passwordMatches)
            throw new InvalidOperationException("Das alte Passwort ist falsch.");

        user.PasswordHash = _passwordService.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }
}