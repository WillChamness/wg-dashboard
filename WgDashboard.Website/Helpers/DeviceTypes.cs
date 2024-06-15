namespace WgDashboard.Website.Helpers
{
    public static class DeviceTypes
    {
        public static readonly string Pc = "PC";
        public static readonly string Laptop = "Laptop";
        public static readonly string Mac = "Mac";
        public static readonly string Phone = "Phone";
        public static readonly string Other = "Other";

        public static bool IsValidDeviceType(string? type)
        {
            return type == null || type == Pc || type == Laptop || type == Mac || type == Phone || type == Other;
        }

        public static string[] AllValidTypes { get => new string[] { Pc, Laptop, Mac, Phone, Other }; }
    }
}
