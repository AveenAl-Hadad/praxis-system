using Praxis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Praxis.Client.Views.Main
{
    public partial class MainWindow
    {
        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await _userManagementService.GetAllUsersAsync();
        }
        public async Task<User> CreateUserAsync(string username, string password, string role)
        {
            return await _userManagementService.CreateUserAsync(username, password, role);
        }
        public async Task UpdateUserRoleAsync(int userId, string role)
        {
            await _userManagementService.UpdateUserRoleAsync(userId, role);
        }
        public async Task ResetUserPasswordAsync(int userId, string newPassword)
        {
            await _userManagementService.ResetPasswordAsync(userId, newPassword);
        }
        public async Task ToggleUserActiveAsync(int userId)
        {
            await _userManagementService.ToggleUserActiveAsync(userId);
        }
        public async Task DeleteUserAsync(int userId)
        {
            await _userManagementService.DeleteUserAsync(userId);
        }
        public async Task OpenUserManagementPageAsync()
        {
            LoadPage(_userManagementPage);
            await _userManagementPage.RefreshAsync();
        }
        public void OpenAddUserPage()
        {
            LoadPage(_addUserPage);
        }
        public void OpenEditUserPage(User user)
        {
            _editUserPage.SetUser(user);
            LoadPage(_editUserPage);
        }
    }
}
