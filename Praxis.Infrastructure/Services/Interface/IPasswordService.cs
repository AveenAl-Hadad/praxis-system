namespace Praxis.Infrastructure.Services.Interface;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}