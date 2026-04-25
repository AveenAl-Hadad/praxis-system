using Praxis.Client.Session;
using Praxis.Domain.Constants;

namespace Praxis.Client.Security;

public static class PermissionHelper
{
    public static bool IsAdmin =>
        UserSession.HasRole(Roles.Administrator) ||
        UserSession.HasRole("Admin");

    public static bool IsDoctor =>
        UserSession.HasRole(Roles.Arzt);

    public static bool IsAssistant =>
        UserSession.HasRole(Roles.Mitarbeiter);

    public static bool CanManageUsers =>
        IsAdmin;

    public static bool CanImportCatalogs =>
        IsAdmin;

    public static bool CanEditCatalogs =>
        IsAdmin || IsDoctor;

    public static bool CanDeletePatients =>
        IsAdmin || IsDoctor;

    public static bool CanEditPatients =>
        IsAdmin || IsDoctor || IsAssistant;

    public static bool CanUseBilling =>
        IsAdmin || IsAssistant;

    public static bool CanUseLab =>
        IsAdmin || IsDoctor || IsAssistant;
}