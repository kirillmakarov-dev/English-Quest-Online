using System.Reflection;

namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Shared internal utilities used by the save-service implementations.
    /// Not part of the public API.
    /// </summary>
    internal static class SaveServiceHelpers
    {
        /// <summary>
        /// Reads the optional static <c>CurrentVersion</c> const declared on
        /// <typeparamref name="T"/>.  Returns 1 when the constant is absent so that
        /// plain data classes without an explicit version are treated as v1.
        /// </summary>
        internal static int GetCurrentVersion<T>()
        {
            FieldInfo field = typeof(T).GetField(
                "CurrentVersion",
                BindingFlags.Public | BindingFlags.Static);

            if (field != null && field.FieldType == typeof(int))
                return (int)field.GetValue(null);

            return 1;
        }
    }
}
