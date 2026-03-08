using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services.Interface;

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
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return null;

        var validPassword = _passwordService.VerifyPassword(password, user.PasswordHash);
        if (!validPassword)
            return null;

        return user;
    }
}