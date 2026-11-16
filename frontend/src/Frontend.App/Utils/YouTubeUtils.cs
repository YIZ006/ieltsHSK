using System.Text.RegularExpressions;

namespace Frontend.App.Utils;

public static class YouTubeUtils
{
    // Regex hỗ trợ tất cả các định dạng URL YouTube phổ biến:
    // watch?v=, youtu.be/, shorts/, embed/, live/, v/
    private static readonly Regex YouTubeRegex = new(
        @"(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=|shorts\/|live\/))([\w-]{11})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Trích xuất sạch mã ID 11 ký tự của video YouTube từ bất kỳ đường dẫn nào,
    /// loại bỏ hoàn toàn các tham số playlist (list=...), tracking (si=...), thời gian bắt đầu (t=...).
    /// </summary>
    public static string ExtractVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        url = url.Trim();

        // Nếu chuỗi truyền vào vốn đã là một Video ID chuẩn (11 ký tự gồm chữ, số, gạch nối, gạch dưới)
        if (Regex.IsMatch(url, @"^[\w-]{11}$"))
        {
            return url;
        }

        var match = YouTubeRegex.Match(url);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        // Fallback kiểm tra thông thường nếu URL chứa v=
        if (url.Contains("v="))
        {
            var afterV = url.Split("v=")[1];
            var cleanId = afterV.Split(new[] { '&', '?', '#', '/' }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (cleanId.Length >= 11) return cleanId.Substring(0, 11);
        }

        // Fallback nếu URL chứa youtu.be/
        if (url.Contains("youtu.be/"))
        {
            var afterBe = url.Split("youtu.be/")[1];
            var cleanId = afterBe.Split(new[] { '?', '&', '#', '/' }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (cleanId.Length >= 11) return cleanId.Substring(0, 11);
        }

        return string.Empty;
    }

    /// <summary>
    /// Tạo URL nhúng chuẩn cho iframe:
    /// - rel=0: Không gợi ý video từ kênh khác
    /// - loop=1&playlist={id}: BẮT BUỘC để khóa chặt video, khi hết bài sẽ phát lại chính nó thay vì bị YouTube auto-next sang video ngẫu nhiên khác
    /// - enablejsapi=1: Hỗ trợ IFrame API nếu cần
    /// </summary>
    public static string BuildEmbedUrl(string? urlOrId)
    {
        var videoId = ExtractVideoId(urlOrId);
        if (string.IsNullOrEmpty(videoId)) return string.Empty;

        return $"https://www.youtube.com/embed/{videoId}?rel=0&loop=1&playlist={videoId}&enablejsapi=1";
    }
}
