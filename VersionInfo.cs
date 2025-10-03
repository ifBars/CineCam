namespace CineCam
{
    /// <summary>
    /// Global version information for the CineCam mod.
    /// This is the single source of truth for version numbers across the project.
    /// To update the version, only change the values in this class.
    /// </summary>
    public static class VersionInfo
    {
        // ===== CHANGE VERSION HERE =====
        // Major version number - increment for breaking changes
        public const int Major = 1;

        // Minor version number - increment for new features
        public const int Minor = 0;

        // Patch version number - increment for bug fixes
        public const int Patch = 0;
        // ================================

        // ===== COMPILE-TIME CONSTANTS FOR ATTRIBUTES =====
        /// <summary>
        /// Compile-time constant version string for use in attributes (e.g. "1.0.0")
        /// </summary>
        public const string VERSION_STRING = "1.0.0";
        // ================================================

        /// <summary>
        /// Short version string (e.g. "1.0.0")
        /// </summary>
        public static readonly string Version = $"{Major}.{Minor}.{Patch}";

        /// <summary>
        /// Version string for display in UI (e.g. "Version: 1.0.0")
        /// </summary>
        public static readonly string DisplayVersion = $"Version: {Version}";

        /// <summary>
        /// Assembly version for MelonInfo attribute - use VERSION_STRING for attributes
        /// </summary>
        public static readonly string MelonVersion = Version;

        /// <summary>
        /// Assembly version attribute string (e.g. "1.0.0.0")
        /// </summary>
        public static readonly string AssemblyVersion = Version;

        /// <summary>
        /// File version attribute string (e.g. "1.0.0.0")
        /// </summary>
        public static readonly string FileVersion = Version;

        /// <summary>
        /// Gets a detailed version string with additional information
        /// </summary>
        /// <param name="includeAuthor">Include author information</param>
        /// <param name="includeDescription">Include description</param>
        /// <returns>Detailed version string</returns>
        public static string GetDetailedVersion(bool includeAuthor = false, bool includeDescription = false)
        {
            var result = $"CineCam {Version}";

            if (includeAuthor)
                result += " by Bars";

            if (includeDescription)
                result += " - Cinematic Camera Tool for Schedule I";

            return result;
        }

        /// <summary>
        /// Checks if this version is newer than another version string
        /// </summary>
        /// <param name="otherVersion">Version string to compare against (e.g. "1.0.0")</param>
        /// <returns>True if this version is newer</returns>
        public static bool IsNewerThan(string otherVersion)
        {
            try
            {
                var parts = otherVersion.Split('.');
                if (parts.Length < 3) return true;

                var otherMajor = int.Parse(parts[0]);
                var otherMinor = int.Parse(parts[1]);
                var otherPatch = int.Parse(parts[2]);

                if (Major > otherMajor) return true;
                if (Major < otherMajor) return false;

                if (Minor > otherMinor) return true;
                if (Minor < otherMinor) return false;

                return Patch > otherPatch;
            }
            catch
            {
                return false;
            }
        }
    }
}
