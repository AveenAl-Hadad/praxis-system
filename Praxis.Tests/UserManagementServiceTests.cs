using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using Xunit;

namespace Praxis.Tests;

/// <summary>
/// Diese Testklasse prüft die Funktionen des UserManagementService.
/// 
/// Getestet werden unter anderem:
/// - Benutzer abrufen
/// - Benutzer erstellen
/// - Rollen ändern
/// - Passwörter zurücksetzen
/// - Benutzer löschen
/// 
/// Für die Tests wird eine InMemory-Datenbank verwendet,
/// damit keine echte Datenbank benötigt wird.
/// </summary>
public class UserManagementServiceTests
{
    /// <summary>
    /// Erstellt für jeden Test eine eigene InMemory-Datenbank.
    /// Dadurch beeinflussen sich die Tests nicht gegenseitig.
    /// </summary>
    /// <param name="dbName">Optionaler Name der Datenbank.</param>
    /// <returns>Eine neue Instanz von PraxisDbContext.</returns>
    private static PraxisDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new PraxisDbContext(options);
    }

    /// <summary>
    /// Hilfsmethode zum schnellen Erzeugen eines Test-Benutzers.
    /// Standardwerte werden gesetzt, können aber überschrieben werden.
    /// </summary>
    /// <param name="username">Benutzername des Test-Benutzers.</param>
    /// <param name="passwordHash">Passwort-Hash des Test-Benutzers.</param>
    /// <param name="role">Rolle des Test-Benutzers.</param>
    /// <returns>Ein neues User-Objekt.</returns>
    private static User CreateUser(
        string username = "max",
        string passwordHash = "HASHED",
        string role = Roles.Mitarbeiter)
    {
        return new User
        {
            Username = username,
            PasswordHash = passwordHash,
            Role = role
        };
    }

    /// <summary>
    /// Testet, ob alle Benutzer korrekt aus der Datenbank geladen
    /// und alphabetisch nach Username sortiert zurückgegeben werden.
    /// </summary>
    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnUsersOrderedByUsername()
    {
        // Arrange:
        // Drei Benutzer in unsortierter Reihenfolge speichern
        using var context = CreateInMemoryContext();

        context.Users.AddRange(
            CreateUser("zara"),
            CreateUser("anna"),
            CreateUser("max"));
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();
        var service = new UserManagementService(context, passwordMock.Object);

        // Act:
        // Alle Benutzer vom Service abrufen
        var result = await service.GetAllUsersAsync();

        // Assert:
        // Es sollen 3 Benutzer zurückkommen, alphabetisch sortiert
        Assert.Equal(3, result.Count);
        Assert.Equal("anna", result[0].Username);
        Assert.Equal("max", result[1].Username);
        Assert.Equal("zara", result[2].Username);
    }

    /// <summary>
    /// Testet, ob ein neuer Benutzer korrekt erstellt wird
    /// und das Passwort vorher gehasht gespeichert wird.
    /// </summary>
    [Fact]
    public async Task CreateUserAsync_ShouldCreateUser_WithHashedPassword()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var passwordMock = new Mock<IPasswordService>();
        passwordMock.Setup(p => p.HashPassword("1234")).Returns("HASH_1234");

        var service = new UserManagementService(context, passwordMock.Object);

        // Act:
        // Neuen Benutzer erstellen
        var user = await service.CreateUserAsync("max", "1234", Roles.Mitarbeiter);

        // Assert:
        // Prüfen, ob die Rückgabe korrekt ist
        Assert.NotNull(user);
        Assert.Equal("max", user.Username);
        Assert.Equal("HASH_1234", user.PasswordHash);
        Assert.Equal(Roles.Mitarbeiter, user.Role);

        // Prüfen, ob der Benutzer wirklich in der Datenbank gespeichert wurde
        var savedUser = await context.Users.FirstAsync();
        Assert.Equal("max", savedUser.Username);
        Assert.Equal("HASH_1234", savedUser.PasswordHash);
        Assert.Equal(Roles.Mitarbeiter, savedUser.Role);
    }

    /// <summary>
    /// Testet, ob Leerzeichen bei Benutzername, Passwort und Rolle
    /// vor der Verarbeitung entfernt werden.
    /// </summary>
    [Fact]
    public async Task CreateUserAsync_ShouldTrimUsernamePasswordAndRole()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var passwordMock = new Mock<IPasswordService>();
        passwordMock.Setup(p => p.HashPassword("1234")).Returns("HASH_1234");

        var service = new UserManagementService(context, passwordMock.Object);

        // Act:
        // Eingaben mit zusätzlichen Leerzeichen übergeben
        var user = await service.CreateUserAsync("  max  ", " 1234 ", $"  {Roles.Arzt}  ");

        // Assert:
        // Username und Rolle werden getrimmt,
        // Passwort wird vor dem Hashen ebenfalls getrimmt
        Assert.Equal("max", user.Username);
        Assert.Equal("HASH_1234", user.PasswordHash);
        Assert.Equal(Roles.Arzt, user.Role);
    }

    /// <summary>
    /// Testet, ob das Erstellen eines Benutzers fehlschlägt,
    /// wenn der Benutzername bereits existiert.
    /// Dabei wird Groß-/Kleinschreibung ignoriert.
    /// </summary>
    [Fact]
    public async Task CreateUserAsync_ShouldThrow_WhenUsernameAlreadyExists_CaseInsensitive()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        // Bereits vorhandener Benutzer
        context.Users.Add(CreateUser("Max"));
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();
        var service = new UserManagementService(context, passwordMock.Object);

        // Act + Assert:
        // "max" soll als gleich zu "Max" erkannt werden
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateUserAsync("max", "1234", Roles.Mitarbeiter));

        Assert.Equal("Benutzername existiert bereits.", ex.Message);
    }

    /// <summary>
    /// Testet, ob beim Erstellen eines Benutzers
    /// eine ungültige Rolle korrekt abgefangen wird.
    /// </summary>
    [Fact]
    public async Task CreateUserAsync_ShouldThrow_WhenRoleIsInvalid()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var passwordMock = new Mock<IPasswordService>();
        var service = new UserManagementService(context, passwordMock.Object);

        // Act + Assert:
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateUserAsync("max", "1234", "Chef"));

        Assert.Equal("Ungültige Rolle.", ex.Message);
    }

    /// <summary>
    /// Testet, ob die Rolle eines vorhandenen Benutzers
    /// erfolgreich geändert wird.
    /// </summary>
    [Fact]
    public async Task UpdateUserRoleAsync_ShouldChangeRole()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var user = CreateUser("max", "HASHED", Roles.Mitarbeiter);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();
        var service = new UserManagementService(context, passwordMock.Object);

        // Act:
        // Rolle des Benutzers ändern
        await service.UpdateUserRoleAsync(user.Id, Roles.Arzt);

        // Assert:
        // Prüfen, ob die Rolle in der Datenbank geändert wurde
        var updatedUser = await context.Users.FirstAsync();
        Assert.Equal(Roles.Arzt, updatedUser.Role);
    }

    /// <summary>
    /// Testet, ob beim Ändern der Rolle ein Fehler geworfen wird,
    /// wenn der Benutzer nicht existiert.
    /// </summary>
    [Fact]
    public async Task UpdateUserRoleAsync_ShouldThrow_WhenUserDoesNotExist()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var passwordMock = new Mock<IPasswordService>();
        var service = new UserManagementService(context, passwordMock.Object);

        // Act + Assert:
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateUserRoleAsync(999, Roles.Administrator));

        Assert.Equal("Benutzer wurde nicht gefunden.", ex.Message);
    }

    /// <summary>
    /// Testet, ob beim Ändern der Rolle eine Exception ausgelöst wird,
    /// wenn die neue Rolle ungültig ist.
    /// </summary>
    [Fact]
    public async Task UpdateUserRoleAsync_ShouldThrow_WhenRoleIsInvalid()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();
        var service = new UserManagementService(context, passwordMock.Object);

        // Act + Assert:
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateUserRoleAsync(user.Id, "Gast"));

        Assert.Equal("Ungültige Rolle.", ex.Message);
    }

    /// <summary>
    /// Testet, ob das Passwort eines Benutzers korrekt
    /// durch einen neuen Passwort-Hash ersetzt wird.
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_ShouldUpdatePasswordHash()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var user = CreateUser("max", "OLD_HASH", Roles.Mitarbeiter);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();
        passwordMock.Setup(p => p.HashPassword("neu123")).Returns("NEW_HASH");

        var service = new UserManagementService(context, passwordMock.Object);

        // Act:
        // Passwort zurücksetzen
        await service.ResetPasswordAsync(user.Id, "neu123");

        // Assert:
        // Prüfen, ob der neue Hash gespeichert wurde
        var updatedUser = await context.Users.FirstAsync();
        Assert.Equal("NEW_HASH", updatedUser.PasswordHash);
    }

    /// <summary>
    /// Testet, ob Leerzeichen im neuen Passwort entfernt werden,
    /// bevor es gehasht und gespeichert wird.
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_ShouldTrimNewPassword()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var user = CreateUser("max", "OLD_HASH", Roles.Mitarbeiter);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();
        passwordMock.Setup(p => p.HashPassword("neu123")).Returns("NEW_HASH");

        var service = new UserManagementService(context, passwordMock.Object);

        // Act:
        // Passwort mit Leerzeichen übergeben
        await service.ResetPasswordAsync(user.Id, "  neu123  ");

        // Assert:
        // Prüfen, ob das getrimmte Passwort gehasht wurde
        var updatedUser = await context.Users.FirstAsync();
        Assert.Equal("NEW_HASH", updatedUser.PasswordHash);
    }

    /// <summary>
    /// Testet, ob beim Zurücksetzen des Passworts
    /// ein leerer oder nur aus Leerzeichen bestehender Wert abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_ShouldThrow_WhenPasswordIsEmpty()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();
        var service = new UserManagementService(context, passwordMock.Object);

        // Act + Assert:
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ResetPasswordAsync(user.Id, "   "));

        Assert.Equal("Neues Passwort darf nicht leer sein.", ex.Message);
    }

    /// <summary>
    /// Testet, ob beim Zurücksetzen des Passworts
    /// ein Fehler ausgelöst wird, wenn der Benutzer nicht existiert.
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_ShouldThrow_WhenUserDoesNotExist()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var passwordMock = new Mock<IPasswordService>();
        var service = new UserManagementService(context, passwordMock.Object);

        // Act + Assert:
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResetPasswordAsync(999, "neu123"));

        Assert.Equal("Benutzer wurde nicht gefunden.", ex.Message);
    }

    /// <summary>
    /// Testet, ob ein vorhandener Benutzer erfolgreich
    /// aus der Datenbank gelöscht wird.
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_ShouldRemoveUser_WhenUserExists()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var user = CreateUser("max");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();
        var service = new UserManagementService(context, passwordMock.Object);

        // Act:
        // Benutzer löschen
        await service.DeleteUserAsync(user.Id);

        // Assert:
        // Die Benutzertabelle soll leer sein
        Assert.Empty(context.Users);
    }

    /// <summary>
    /// Testet, ob das Löschen eines nicht vorhandenen Benutzers
    /// keinen Fehler auslöst und die Datenbank unverändert bleibt.
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_ShouldDoNothing_WhenUserDoesNotExist()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var passwordMock = new Mock<IPasswordService>();
        var service = new UserManagementService(context, passwordMock.Object);

        // Act:
        // Nicht vorhandenen Benutzer löschen
        await service.DeleteUserAsync(999);

        // Assert:
        // Es soll kein Fehler passieren und die DB bleibt leer
        Assert.Empty(context.Users);
    }
}