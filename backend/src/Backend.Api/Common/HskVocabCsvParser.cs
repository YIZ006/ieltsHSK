namespace Backend.Api.Common;

/// <summary>
/// Parser CSV hỗ trợ dấu ngoặc kép, dấu phẩy/chấm phẩy/tab trong ô và tự dò delimiter.
/// </summary>
public static class HskVocabCsvParser
{
    public static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    public static IEnumerable<List<string>> Parse(string content)
    {
        char delimiter = DetectDelimiter(content);

        var rows = new List<List<string>>();
        var field = new System.Text.StringBuilder();
        var current = new List<string>();
        var inQuotes = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else inQuotes = false;
                }
                else field.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == delimiter)
            {
                current.Add(field.ToString());
                field.Clear();
            }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n') i++;
                current.Add(field.ToString());
                field.Clear();
                rows.Add(current);
                current = new List<string>();
            }
            else field.Append(c);
        }

        if (field.Length > 0 || current.Count > 0)
        {
            current.Add(field.ToString());
            rows.Add(current);
        }
        return rows;
    }

    private static char DetectDelimiter(string content)
    {
        int commas = 0, semicolons = 0, tabs = 0;
        bool inQuotes = false;
        foreach (char c in content)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (!inQuotes)
            {
                if (c == ',') commas++;
                else if (c == ';') semicolons++;
                else if (c == '\t') tabs++;
                else if (c == '\n') break; // chỉ xét dòng đầu
            }
        }
        if (semicolons > commas && semicolons >= tabs) return ';';
        if (tabs > commas && tabs > semicolons) return '\t';
        return ',';
    }
}
