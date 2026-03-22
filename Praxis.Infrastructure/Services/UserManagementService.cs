using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

/// <summary>
/// Service zur Verwaltung von Benutzern (Admin-Funktionen).
/// Enthält Erstellung, Rollenänderung, Passwort-Reset und Löschen.
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly PraxisDbContext _context;
    private readonly IPasswordService _passwordService;

    /// <summary>
    /// Konstruktor mit Dependency Injection.
    /// </summary>
    public UserManagementService(PraxisDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    /// <summary>
    /// Gibt alle Benutzer sortiert nach Username zurück.
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .AsNoTracking() // Performance bei Read-Only
            .OrderBy(u => u.Username)
            .ToListAsync();
    }

    /// <summary>
    /// Erstellt einen neuen Benutzer.
    /// </summary>
    public async Task<User> CreateUserAsync(string username, string password, string role)
    {
        // Eingaben normalisieren
        username = (username ?? "").Trim();
        password = (password ?? "").Trim();
        role = (role ?? "").Trim();

        // Validierung
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Benutzername darf nicht leer sein.");

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Passwort darf nicht leer sein.");

        // Rollenprüfung
        if (role != Roles.Administrator &&
            role != Roles.Mitarbeiter &&
            role != Roles.Arzt)
        {
            throw new ArgumentException("Ungültige Rolle.");
        }

        // Case-insensitive prüfen (besser!)
        var exists = await _context.Users
            .AnyAsync(u => u.Username.ToLower() == username.ToLower());

        if (exists)
            throw new InvalidOperationException("Benutzername existiert bereits.");

        // Benutzer erstellen
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

    /// <summary>
    /// Ändert die Rolle eines Benutzers.
    /// </summary>
    public async Task UpdateUserRoleAsync(int userId, string role)
    {
        role = (role ?? "").Trim();

        // 🔥 BUGFIX: Arzt-Rolle erlaubt (war vorher vergessen!)
        if (role != Roles.Administrator &&
            role != Roles.Mitarbeiter &&
            role != Roles.Arzt)
        {
            throw new ArgumentException("Ungültige Rolle.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new InvalidOperationException("Benutzer wurde nicht gefunden.");

        user.Role = role;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Setzt das Passwort eines Benutzers zurück.
    /// </summary>
    public async Task ResetPasswordAsync(int userId, string newPassword)
    {
        newPassword = (newPassword ?? "").Trim();

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("Neues Passwort darf nicht leer sein.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new InvalidOperationException("Benutzer wurde nicht gefunden.");

        user.PasswordHash = _passwordService.HashPassword(newPassword);

        await _context.SaveChangesAsync();

        // verhindert Tracking-Probleme bei weiteren Operationen
        _context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Löscht einen Benutzer.
    /// </summary>
    public async Task DeleteUserAsync(int userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
}