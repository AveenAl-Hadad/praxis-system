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

public class UserManagementServiceTests
{
    // Erstellt eine InMemory-Datenbank für Tests.
    // Jeder Test bekommt eine eigene Datenbank.
    private static PraxisDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new PraxisDbContext(options);
    }

    // Hilfsmethode: Beispiel-Benutzer
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
        // Alle Benutzer laden
        var result = await service.GetAllUsersAsync();

        // Assert:
        // Es sollen 3 Benutzer zurückkommen, alphabetisch sortiert
        Assert.Equal(3, result.Count);
        Assert.Equal("anna", result[0].Username);
        Assert.Equal("max", result[1].Username);
        Assert.Equal("zara", result[2].Username);
    }

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
        // Prüfen, ob die Daten korrekt übernommen wurden
        Assert.NotNull(user);
        Assert.Equal("max", user.Username);
        Assert.Equal("HASH_1234", user.PasswordHash);
        Assert.Equal(Roles.Mitarbeiter, user.Role);

        // Prüfen, ob er wirklich in der Datenbank gespeichert wurde
        var savedUser = await context.Users.FirstAsync();
        Assert.Equal("max", savedUser.Username);
        Assert.Equal("HASH_1234", savedUser.PasswordHash);
        Assert.Equal(Roles.Mitarbeiter, savedUser.Role);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldTrimUsernamePasswordAndRole()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var passwordMock = new Mock<IPasswordService>();
        passwordMock.Setup(p => p.HashPassword("1234")).Returns("HASH_1234");

        var service = new UserManagementService(context, passwordMock.Object);

        // Act:
        // Eingaben mit Leerzeichen übergeben
        var user = await service.CreateUserAsync("  max  ", " 1234 ", $"  {Roles.Arzt}  ");

        // Assert:
        // Username und Rolle werden im Service getrimmt,
        // Passwort wird getrimmt bevor es gehasht wird
        Assert.Equal("max", user.Username);
        Assert.Equal("HASH_1234", user.PasswordHash);
        Assert.Equal(Roles.Arzt, user.Role);
    }

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
        // Rolle ändern
        await service.UpdateUserRoleAsync(user.Id, Roles.Arzt);

        // Assert:
        var updatedUser = await context.Users.FirstAsync();
        Assert.Equal(Roles.Arzt, updatedUser.Role);
    }

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
        var updatedUser = await context.Users.FirstAsync();
        Assert.Equal("NEW_HASH", updatedUser.PasswordHash);
    }

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
        await service.ResetPasswordAsync(user.Id, "  neu123  ");

        // Assert:
        var updatedUser = await context.Users.FirstAsync();
        Assert.Equal("NEW_HASH", updatedUser.PasswordHash);
    }

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
        await service.DeleteUserAsync(user.Id);

        // Assert:
        Assert.Empty(context.Users);
    }

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