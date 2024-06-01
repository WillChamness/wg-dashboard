namespace WgDashboardWebsite.Helpers
{
    public static class StringsNullOrEmpty
    {
        public static bool Any(params string?[] strings)
        {
            for(int i = 0; i < strings.Length; i++)
            {
                if (string.IsNullOrEmpty(strings[i]))
                    return true;
            }
            return false;
        }
    }
}
