using YoutubeExplode;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Backend.Infrastructure.Services;

public class YoutubeTranscriptService
{
    public (string JsonContent, int WordCount) ParseRawTextToTranscriptJson(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return ("{\"Words\":[]}", 0);

        // Loại bỏ các mốc thời gian dạng 00:12, 0:15, 1:23:45 nếu user copy từ giao diện Youtube
        var cleanText = Regex.Replace(rawText, @"\b\d{1,2}:\d{2}(?::\d{2})?\b", " ");
        var words = cleanText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        var transcriptWords = new List<TranscriptWord>();
        int count = 0;
        double currentStart = 0.0;
        
        foreach (var w in words)
        {
            transcriptWords.Add(new TranscriptWord
            {
                Text = w,
                Start = Math.Round(currentStart, 2),
                Duration = 0.4
            });
            currentStart += 0.4;
            count++;
        }
        
        var json = JsonSerializer.Serialize(new { Words = transcriptWords });
        return (json, count);
    }

    public async Task<(string Title, string ChannelName, TimeSpan Duration, string ThumbnailUrl)> GetVideoInfoAsync(string videoUrl)
    {
        try
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            var youtube = new YoutubeClient(httpClient);
            var video = await youtube.Videos.GetAsync(videoUrl);
            
            var title = video.Title;
            var channel = video.Author.ChannelTitle;
            var duration = video.Duration ?? TimeSpan.Zero;
            var thumbnail = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault()?.Url ?? "";
            
            return (title, channel, duration, thumbnail);
        }
        catch
        {
            var videoId = ExtractVideoId(videoUrl);
            return ("Video Youtube mới", "Youtube Channel", TimeSpan.FromMinutes(5), string.IsNullOrEmpty(videoId) ? "" : $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg");
        }
    }

    private static readonly Regex YouTubeRegex = new(
        @"(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=|shorts\/|live\/))([\w-]{11})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string ExtractVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        url = url.Trim();
        if (Regex.IsMatch(url, @"^[\w-]{11}$")) return url;

        var match = YouTubeRegex.Match(url);
        if (match.Success) return match.Groups[1].Value;

        if (url.Contains("v="))
        {
            var afterV = url.Split("v=")[1];
            var cleanId = afterV.Split(new[] { '&', '?', '#', '/' }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (cleanId.Length >= 11) return cleanId.Substring(0, 11);
        }

        if (url.Contains("youtu.be/"))
        {
            var afterBe = url.Split("youtu.be/")[1];
            var cleanId = afterBe.Split(new[] { '?', '&', '#', '/' }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (cleanId.Length >= 11) return cleanId.Substring(0, 11);
        }

        return string.Empty;
    }
}

public class TranscriptWord
{
    public string Text { get; set; } = string.Empty;
    public double Start { get; set; }
    public double Duration { get; set; }
}
