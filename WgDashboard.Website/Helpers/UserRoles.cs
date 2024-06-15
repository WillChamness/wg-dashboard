namespace WgDashboard.Website.Helpers
{
    public static class UserRoles
    {
        public static readonly string Anonymous = "anonymous";
        public static readonly string User = "user";
        public static readonly string Admin = "admin";

        public static bool IsValidRole(string? role) => (role == Anonymous) || (role == User) || (role == Admin);
    }
}
