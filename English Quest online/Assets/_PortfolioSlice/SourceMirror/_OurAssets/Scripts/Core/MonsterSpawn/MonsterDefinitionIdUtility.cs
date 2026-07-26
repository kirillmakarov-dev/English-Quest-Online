using System.Text;

/// <summary>
/// Helpers for stable monster definition ids.
/// </summary>
public static class MonsterDefinitionIdUtility
{
    public static string DeriveIdFromAssetName(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
            return string.Empty;

        string name = assetName;
        if (name.StartsWith("Monster_"))
            name = name.Substring("Monster_".Length);

        if (name.EndsWith("_Definition"))
            name = name.Substring(0, name.Length - "_Definition".Length);

        return ToSnakeCase(name);
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var builder = new StringBuilder(input.Length + 4);
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c) && i > 0)
                builder.Append('_');

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}
