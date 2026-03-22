using System.Security.Cryptography;
using System.Text;

namespace Praxis.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    // Methode zum Hashen eines Passworts
    public string HashPassword(string password)
    {
        // Prüfen, ob Passwort leer ist
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Passwort darf nicht leer sein.");

        // SHA256 Hash-Algorithmus erstellen
        using var sha256 = SHA256.Create();

        // Passwort in Bytes umwandeln (UTF-8 Encoding)
        var bytes = Encoding.UTF8.GetBytes(password);

        // Hash berechnen
        var hash = sha256.ComputeHash(bytes);

        // Hash als Base64 String zurückgeben (für Speicherung in DB)
        return Convert.ToBase64String(hash);
    }

    // Methode zum Überprüfen eines Passworts beim Login
    public bool VerifyPassword(string password, string passwordHash)
    {
        // Das eingegebene Passwort wird erneut gehasht
        var hashedInput = HashPassword(password);

        // Vergleich: stimmt der neue Hash mit dem gespeicherten überein?
        return hashedInput == passwordHash;
    }
}