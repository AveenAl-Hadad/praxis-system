using Praxis.Domain.Entities;

namespace Praxis.Client.Session;

public static class UserSession
{
    public static User? CurrentUser { get; private set; }

    public static bool IsLoggedIn => CurrentUser != null;

    public static void Login(User user)
    {
        CurrentUser = user;
    }

    public static void Logout()
    {
        CurrentUser = null;
    }

    
    public static bool HasRole(string role)
    {
        return CurrentUser != null &&
               string.Equals(CurrentUser.Role, role, StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasAnyRole(params string[] roles)
    {
        return CurrentUser != null &&
               roles.Any(r => string.Equals(CurrentUser.Role, r, StringComparison.OrdinalIgnoreCase));
    }

}