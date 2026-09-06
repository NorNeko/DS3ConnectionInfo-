using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Media;

namespace DS3ConnectionInfo
{
    internal static class ColumnSettings
    {
        internal const int Count = 20;

        internal static StringCollection Normalize(StringCollection values)
        {
            var result = new StringCollection();
            for (int i = 0; i < Count; i++)
            {
                string value = values != null && i < values.Count ? values[i] : "Hidden";
                result.Add(value == "Visible" ? "Visible" : "Hidden");
            }
            return result;
        }

        internal static StringCollection NormalizeOrder(StringCollection values)
        {
            var result = new StringCollection();
            var seen = new HashSet<int>();
            if (values != null) foreach (string value in values)
                if (int.TryParse(value, out int index) && index >= 0 && index < Count && seen.Add(index))
                    result.Add(index.ToString());
            for (int i = 0; i < Count; i++) if (seen.Add(i)) result.Add(i.ToString());
            return result;
        }

        internal static StringCollection NormalizeColors(StringCollection values)
        {
            var result = new StringCollection();
            for (int i = 0; i < Count; i++)
            {
                string value = values != null && i < values.Count ? values[i] : "";
                try { value = string.IsNullOrWhiteSpace(value) ? "" : ((Color)ColorConverter.ConvertFromString(value)).ToString(); }
                catch (Exception) { value = ""; }
                result.Add(value);
            }
            return result;
        }

        internal static void EnsureCompatible()
        {
            // Overlay selections are the source of truth, including when upgrading old settings.
            Settings.Default.OverlayColVisibility = Normalize(Settings.Default.OverlayColVisibility);
            Settings.Default.SessColumnVisibility = Normalize(Settings.Default.OverlayColVisibility);
            Settings.Default.SessionColumnOrder = NormalizeOrder(Settings.Default.SessionColumnOrder);
            Settings.Default.OverlayColumnColors = NormalizeColors(Settings.Default.OverlayColumnColors);
        }
    }
}
