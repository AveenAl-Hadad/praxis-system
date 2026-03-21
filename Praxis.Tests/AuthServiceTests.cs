using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using Xunit;

namespace Praxis.Tests;

public class AuthServiceTests
{
    // InMemory-Datenbank für Tests
    private static PraxisDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new PraxisDbContext(options);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldCreateUser_WithHashedPassword()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Mock für Passwort-Service
        var passwordMock = new Mock<IPasswordService>();
        passwordMock.Setup(p => p.HashPassword("1234")).Returns("HASHED");

        var service = new AuthService(context, passwordMock.Object);

        // Act
        var user = await service.RegisterUserAsync("max", "1234", Roles.Mitarbeiter);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("max", user.Username);
        Assert.Equal("HASHED", user.PasswordHash);
        Assert.Equal(Roles.Mitarbeiter, user.Role);

        // Prüfen, ob User wirklich in DB gespeichert wurde
        var savedUser = await context.Users.FirstAsync();
        Assert.Equal("max", savedUser.Username);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldThrow_WhenUsernameExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        context.Users.Add(new User
        {
            Username = "max",
            PasswordHash = "old",
            Role = Roles.Mitarbeiter
        });
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();
        var service = new AuthService(context, passwordMock.Object);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterUserAsync("max", "1234", Roles.Mitarbeiter));

        Assert.Equal("Benutzername existiert bereits.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnUser_WhenPasswordIsCorrect()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        context.Users.Add(new User
        {
            Username = "max",
            PasswordHash = "HASHED",
            Role = Roles.Mitarbeiter
        });
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();
        passwordMock.Setup(p => p.VerifyPassword("1234", "HASHED")).Returns(true);

        var service = new AuthService(context, passwordMock.Object);

        // Act
        var result = await service.LoginAsync("max", "1234");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("max", result!.Username);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenPasswordIsWrong()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        context.Users.Add(new User
        {
            Username = "max",
            PasswordHash = "HASHED",
            Role = Roles.Mitarbeiter
        });
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();
        passwordMock.Setup(p => p.VerifyPassword("wrong", "HASHED")).Returns(false);

        var service = new AuthService(context, passwordMock.Object);

        // Act
        var result = await service.LoginAsync("max", "wrong");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldUpdatePassword_WhenOldPasswordCorrect()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var user = new User
        {
            Username = "max",
            PasswordHash = "OLD_HASH",
            Role = Roles.Mitarbeiter
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();

        // Altes Passwort ist korrekt
        passwordMock.Setup(p => p.VerifyPassword("old", "OLD_HASH")).Returns(true);

        // Neues Passwort wird gehasht
        passwordMock.Setup(p => p.HashPassword("new")).Returns("NEW_HASH");

        var service = new AuthService(context, passwordMock.Object);

        // Act
        await service.ChangePasswordAsync(user.Id, "old", "new");

        // Assert
        var updatedUser = await context.Users.FirstAsync();
        Assert.Equal("NEW_HASH", updatedUser.PasswordHash);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrow_WhenOldPasswordWrong()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        var user = new User
        {
            Username = "max",
            PasswordHash = "OLD_HASH",
            Role = Roles.Mitarbeiter
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordMock = new Mock<IPasswordService>();

        // Altes Passwort ist falsch
        passwordMock.Setup(p => p.VerifyPassword("wrong", "OLD_HASH")).Returns(false);

        var service = new AuthService(context, passwordMock.Object);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangePasswordAsync(user.Id, "wrong", "new"));

        Assert.Equal("Das alte Passwort ist falsch.", ex.Message);
    }
}