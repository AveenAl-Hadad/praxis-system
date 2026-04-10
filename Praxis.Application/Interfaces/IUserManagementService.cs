using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces
{ 
public interface IUserManagementService
{
    Task<List<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(string username, string password, string role);
    Task UpdateUserRoleAsync(int userId, string role);
    Task ResetPasswordAsync(int userId, string newPassword);
    Task DeleteUserAsync(int userId);
    Task ToggleUserActiveAsync(int userId);
}
}