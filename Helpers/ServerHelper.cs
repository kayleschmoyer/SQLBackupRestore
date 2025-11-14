using System;
using SQLBackupRestore.Models;

namespace SQLBackupRestore.Helpers
{
    /// <summary>
    /// Helper class for SQL Server-related operations.
    /// </summary>
    public static class ServerHelper
    {
        /// <summary>
        /// Determines the box type based on the server name pattern.
        /// </summary>
        /// <param name="serverName">The SQL Server instance name.</param>
        /// <returns>The detected box type.</returns>
        /// <remarks>
        /// Boxes with "ST" in the name (e.g., USATSVASTQAST01, USATSVASTQAST02) are Office boxes.
        /// Boxes with "SH" in the name (e.g., USATSVASTQASH01, USATSVASTQASH02) are Shop boxes.
        /// Others (e.g., USATSVASTSQL02) are considered General boxes that can have either type.
        /// </remarks>
        public static BoxType DetectBoxType(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                return BoxType.General;
            }

            // Convert to uppercase for case-insensitive comparison
            var upperServerName = serverName.ToUpperInvariant();

            // Check for Shop pattern - servers with "SH" that are specifically Shop boxes
            // Examples: USATSVASTQASH01, USATSVASTQASH02, USATSVASTQASH03
            // Look for patterns like "QASH", "TASH", or other "SH" patterns that indicate Shop
            if (upperServerName.Contains("QASH") || upperServerName.Contains("TASH"))
            {
                return BoxType.Shop;
            }

            // Check for Office pattern - servers with "ST" that are specifically Office boxes
            // Examples: USATSVASTQAST01, USATSVASTQAST02, USATSVASTQAST03
            // Look for patterns like "QAST", "TAST" that indicate Office
            if (upperServerName.Contains("QAST") || upperServerName.Contains("TAST"))
            {
                return BoxType.Office;
            }

            // Default to General for servers like USATSVASTSQL02 or other non-specific patterns
            return BoxType.General;
        }

        /// <summary>
        /// Gets a suggested database type based on the detected box type.
        /// </summary>
        /// <param name="boxType">The box type.</param>
        /// <returns>The suggested database type, or null if the box is General.</returns>
        public static DatabaseType? GetSuggestedDatabaseType(BoxType boxType)
        {
            return boxType switch
            {
                BoxType.Office => DatabaseType.Office,
                BoxType.Shop => DatabaseType.Shop,
                BoxType.General => null, // No suggestion for general boxes
                _ => null
            };
        }

        /// <summary>
        /// Gets a friendly description of the box type.
        /// </summary>
        /// <param name="boxType">The box type.</param>
        /// <returns>A friendly description string.</returns>
        public static string GetBoxTypeDescription(BoxType boxType)
        {
            return boxType switch
            {
                BoxType.Office => "Office Box (ST pattern detected)",
                BoxType.Shop => "Shop Box (SH pattern detected)",
                BoxType.General => "General Box (can host Office or Shop databases)",
                _ => "Unknown Box Type"
            };
        }
    }
}
