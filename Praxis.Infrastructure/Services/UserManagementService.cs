using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly PraxisDbContext _context;
    private readonly IPasswordService _passwordService;

    public UserManagementService(PraxisDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<User> CreateUserAsync(string username, string password, string role)
    {
        username = (username ?? "").Trim();
        password = (password ?? "").Trim();
        role = (role ?? "").Trim();

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Benutzername darf nicht leer sein.");

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Passwort darf nicht leer sein.");

        if (role != Roles.Administrator && role != Roles.Mitarbeiter && role != Roles.Arzt)
            throw new ArgumentException("Ungültige Rolle.");

        var exists = await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
        if (exists)
            throw new InvalidOperationException("Benutzername existiert bereits.");

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordService.HashPassword(password),
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task UpdateUserRoleAsync(int userId, string role)
    {
        role = (role ?? "").Trim();

        if (role != Roles.Administrator && role != Roles.Mitarbeiter)
            throw new ArgumentException("Ungültige Rolle.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new InvalidOperationException("Benutzer wurde nicht gefunden.");

        user.Role = role;
        await _context.SaveChangesAsync();
    }

    public async Task ResetPasswordAsync(int userId, string newPassword)
    {
        newPassword = (newPassword ?? "").Trim();

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("Neues Passwort darf nicht leer sein.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new InvalidOperationException("Benutzer wurde nicht gefunden.");

        user.PasswordHash = _passwordService.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    public async Task DeleteUserAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
}