using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;
/// <summary>
/// Service für Authentifizierung und Benutzerverwaltung.
/// Zuständig für Registrierung, Login und Passwortänderung.
/// </summary>
public class AuthService : IAuthService
{
    private readonly PraxisDbContext _context;
    private readonly IPasswordService _passwordService;

    /// <summary>
    /// Konstruktor mit Dependency Injection für DbContext und PasswordService.
    /// </summary>
    public AuthService(PraxisDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    /// <summary>
    /// Registriert einen neuen Benutzer.
    /// </summary>
    public async Task<User> RegisterUserAsync(string username, string password, string role)
    {
        // Validierung
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Benutzername darf nicht leer sein.");

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Passwort darf nicht leer sein.");

        // Rollenprüfung (nur erlaubte Rollen)
        if (role != Roles.Administrator && role != Roles.Mitarbeiter)
            throw new ArgumentException("Ungültige Rolle.");

        // Prüfen, ob Benutzername bereits existiert
        var exists = await _context.Users
            .AnyAsync(u => u.Username == username);

        if (exists)
            throw new InvalidOperationException("Benutzername existiert bereits.");

        // Benutzer erstellen
        var user = new User
        {
            Username = username.Trim(),
            PasswordHash = _passwordService.HashPassword(password), // Passwort wird gehasht!
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Führt den Login eines Benutzers durch.
    /// Gibt den Benutzer zurück, wenn erfolgreich, sonst null.
    /// </summary>
    public async Task<User?> LoginAsync(string username, string password)
    {
        // Null-Safety + Trim
        username = (username ?? "").Trim();
        password = (password ?? "").Trim();

        // Leere Eingaben -> kein Login
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        // Benutzer suchen (ohne Tracking für Performance)
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return null;

        // Passwort prüfen (Hash-Vergleich)
        return _passwordService.VerifyPassword(password, user.PasswordHash)
            ? user
            : null;
    }

    /// <summary>
    /// Ändert das Passwort eines Benutzers.
    /// </summary>
    public async Task ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new InvalidOperationException("Benutzer wurde nicht gefunden.");

        // Prüfen, ob das alte Passwort korrekt ist
        var passwordMatches = _passwordService.VerifyPassword(
            oldPassword,
            user.PasswordHash);

        if (!passwordMatches)
            throw new InvalidOperationException("Das alte Passwort ist falsch.");

        // Neues Passwort setzen (gehasht)
        user.PasswordHash = _passwordService.HashPassword(newPassword);

        await _context.SaveChangesAsync();

        // ChangeTracker leeren (verhindert Side Effects bei weiteren Operationen)
        _context.ChangeTracker.Clear();
    }
}