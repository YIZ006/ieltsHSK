using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Backend.Api;

/// <summary>
/// Bộ lọc HTML tối giản chống stored XSS cho nội dung do admin/người dùng soạn
/// (đề thi, transcript, từ vựng...) trước khi lưu. Loại bỏ thẻ nguy hiểm,
/// event handler inline và URI độc hại. Lớp phòng thủ thứ hai sau [Authorize].
/// </summary>
public static partial class HtmlGuard
{
    [GeneratedRegex(@"<\s*(script|iframe|object|embed|form|base|svg|math|link|meta|style)\b.*?<\s*/\s*\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DangerousBlock();

    [GeneratedRegex(@"<\s*/?\s*(script|iframe|object|embed|form|base|svg|math|link|meta|style)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousTag();

    [GeneratedRegex(@"\son[a-z]+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase)]
    private static partial Regex InlineHandler();

    [GeneratedRegex(@"(javascript|vbscript)\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousScheme();

    [GeneratedRegex(@"data\s*:\s*text\s*/\s*html", RegexOptions.IgnoreCase)]
    private static partial Regex DataHtml();

    public static string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;

        var s = DangerousBlock().Replace(input, string.Empty);
        s = DangerousTag().Replace(s, string.Empty);
        s = InlineHandler().Replace(s, string.Empty);
        s = DangerousScheme().Replace(s, "blocked:");
        s = DataHtml().Replace(s, "blocked:");
        return s;
    }

    /// <summary>
    /// Parse JSON, làm sạch mọi giá trị chuỗi bên trong (đệ quy) rồi trả về JSON mới.
    /// Nếu JSON không hợp lệ thì làm sạch như một chuỗi thường.
    /// </summary>
    public static string SanitizeJsonStrings(string json)
    {
        try
        {
            var node = JsonNode.Parse(json);
            if (node == null) return json;
            var clean = SanitizeNode(node);
            return clean?.ToJsonString() ?? json;
        }
        catch
        {
            return Sanitize(json);
        }
    }

    private static JsonNode? SanitizeNode(JsonNode? node)
    {
        if (node == null) return null;

        switch (node)
        {
            case JsonObject obj:
            {
                var newObj = new JsonObject();
                foreach (var prop in obj)
                {
                    newObj[prop.Key] = SanitizeNode(prop.Value);
                }
                return newObj;
            }
            case JsonArray arr:
            {
                var newArr = new JsonArray();
                foreach (var item in arr)
                {
                    newArr.Add(SanitizeNode(item));
                }
                return newArr;
            }
            default:
            {
                if (node is JsonValue value && value.TryGetValue<string>(out var str))
                {
                    return JsonValue.Create(Sanitize(str));
                }
                return JsonNode.Parse(node.ToJsonString());
            }
        }
    }
}
