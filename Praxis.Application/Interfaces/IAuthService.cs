using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces
{ 

public interface IAuthService
{
    Task<User> RegisterUserAsync(string username, string password, string role);
    Task<User?> LoginAsync(string username, string password);
    Task ChangePasswordAsync(int userId, string oldPassword, string newPassword);
}
}