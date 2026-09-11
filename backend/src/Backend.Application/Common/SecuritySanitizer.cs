using System.Text.RegularExpressions;
using System.Web;

namespace Backend.Application.Common;

/// <summary>
/// Tiện ích bảo mật chống XSS, tiêm mã lệnh (Script Injection), thẻ nhúng và các hàm nguy hại.
/// </summary>
public static class SecuritySanitizer
{
    // Regex phát hiện các thẻ mã lệnh, thẻ nhúng và cấu trúc nguy hiểm
    private static readonly Regex DangerousTagsRegex = new(
        @"<\s*(script|iframe|object|embed|applet|meta|link|style|form|input|button|svg|base|body|html)[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ClosingDangerousTagsRegex = new(
        @"<\s*/\s*(script|iframe|object|embed|applet|meta|link|style|form|input|button|svg|base|body|html)[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex phát hiện các event handlers HTML (onclick=, onerror=, onload=, onmouseover=,...)
    private static readonly Regex EventHandlersRegex = new(
        @"\bon[a-zA-Z]+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex phát hiện các URI scheme thực thi mã (javascript:, vbscript:, data:text/html)
    private static readonly Regex ScriptSchemesRegex = new(
        @"(javascript|vbscript|data\s*:\s*text\/html)\s*:",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex phát hiện các hàm / thuộc tính nguy hại thường dùng trong XSS
    private static readonly Regex DangerousFunctionsRegex = new(
        @"(eval\s*\(|document\s*\.\s*(cookie|location|domain)|window\s*\.\s*location)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Kiểm tra xem chuỗi có chứa bất kỳ dấu hiệu mã độc / mã lệnh nào không
    /// </summary>
    public static bool ContainsDangerousScript(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        // Giải mã URL-encoded và HTML entities để bắt các thủ thuật mã hóa bypass (ví dụ %3Cscript%3E hoặc &lt;script&gt;)
        string decoded = input;
        try
        {
            decoded = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(input));
        }
        catch
        {
            decoded = input;
        }

        return DangerousTagsRegex.IsMatch(decoded)
            || ClosingDangerousTagsRegex.IsMatch(decoded)
            || EventHandlersRegex.IsMatch(decoded)
            || ScriptSchemesRegex.IsMatch(decoded)
            || DangerousFunctionsRegex.IsMatch(decoded);
    }

    /// <summary>
    /// Kiểm tra nghiêm ngặt: Ném ngoại lệ ArgumentException nếu phát hiện mã độc
    /// </summary>
    public static void ValidateSafeText(string? input, string fieldName = "Dữ liệu")
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        if (ContainsDangerousScript(input))
        {
            throw new ArgumentException($"{fieldName} chứa ký tự hoặc mã lệnh không hợp lệ. Vui lòng kiểm tra lại.");
        }
    }

    /// <summary>
    /// Làm sạch chuỗi văn bản, loại bỏ các thẻ HTML và đoạn mã nguy hại
    /// </summary>
    public static string SanitizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var cleaned = input;
        cleaned = DangerousTagsRegex.Replace(cleaned, string.Empty);
        cleaned = ClosingDangerousTagsRegex.Replace(cleaned, string.Empty);
        cleaned = EventHandlersRegex.Replace(cleaned, string.Empty);
        cleaned = ScriptSchemesRegex.Replace(cleaned, string.Empty);
        cleaned = DangerousFunctionsRegex.Replace(cleaned, string.Empty);

        return cleaned.Trim();
    }
}
