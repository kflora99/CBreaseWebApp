using System.Reflection;

namespace CBreaseWebApp1
{
    public static class AppInfo
    {
        private const string BuildTimestampMetadataKey = "BuildTimestampUtc";

        private static readonly Assembly Assembly = typeof(AppInfo).Assembly;

        public static string Version
        {
            get
            {
                var informationalVersion = Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrWhiteSpace(informationalVersion))
                {
                    return informationalVersion;
                }

                return Assembly.GetName().Version?.ToString() ?? "unknown";
            }
        }

        public static string BuildTimestampUtc =>
            Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == BuildTimestampMetadataKey)
                ?.Value
            ?? "unknown";
    }
}
