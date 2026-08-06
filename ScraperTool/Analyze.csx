using System;
using System.IO;
using System.Text.RegularExpressions;

// Quick HTML structure analyzer - reads part1.html and shows div structure
string html = File.ReadAllText("part1.html");

// Find all divs with class containing "test-panel" or "part" or "take-test"
var pattern = new Regex(@"<div[^>]*class=""([^""]*(?:test-panel|take-test|region-content|passage)[^""]*)""[^>]*>", RegexOptions.IgnoreCase);

var matches = pattern.Matches(html);
Console.WriteLine($"=== CÁC DIV LIÊN QUAN ({matches.Count} kết quả) ===\n");

int count = 0;
foreach (Match m in matches)
{
    if (count++ > 30) break;
    var cls = m.Groups[1].Value;
    var pos = m.Index;
    Console.WriteLine($"[{pos}] class=\"{cls}\"");
}

// Count test-panel__item
int panelCount = Regex.Matches(html, "test-panel__item").Count;
int passageCount = Regex.Matches(html, "passage-content").Count;
int splitCount = Regex.Matches(html, "take-test__split-item").Count;

Console.WriteLine($"\n=== THỐNG KÊ ===");
Console.WriteLine($"test-panel__item: {panelCount} lần");
Console.WriteLine($"passage-content: {passageCount} lần");
Console.WriteLine($"take-test__split-item: {splitCount} lần");

// Find where test-panel__header appears (each Part header)
var headerPattern = new Regex(@"test-panel__header[^>]*>.*?</div>", RegexOptions.Singleline);
var headers = headerPattern.Matches(html);
Console.WriteLine($"\n=== HEADER CỦA TỪNG NHÓM ({headers.Count} headers) ===");
foreach (Match h in headers.Cast<Match>().Take(10))
    Console.WriteLine(h.Value.Substring(0, Math.Min(200, h.Value.Length)));
