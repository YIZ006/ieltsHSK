namespace Backend.Api.Common;

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

public record HskParsedRow(
    string Level,
    string Hanzi,
    string Pinyin,
    string Meaning,
    string? WordType,
    string? ExampleSentence,
    string? ExamplePinyin,
    string? ExampleMeaning,
    string? AudioUrl,
    int DisplayOrder,
    string? Topic
);

public class ColumnMapping
{
    public int LevelCol { get; set; } = -1;
    public int HanziCol { get; set; } = -1;
    public int PinyinCol { get; set; } = -1;
    public int MeaningCol { get; set; } = -1;
    public int WordTypeCol { get; set; } = -1;
    public int ExampleCol { get; set; } = -1;
    public int ExamplePinyinCol { get; set; } = -1;
    public int ExampleMeaningCol { get; set; } = -1;
    public int AudioCol { get; set; } = -1;
    public int OrderCol { get; set; } = -1;
    public int TopicCol { get; set; } = -1;
}

/// <summary>
/// Parser CSV & Excel thông minh cho từ vựng HSK: tự dò delimiter, tự nhận diện header đa ngôn ngữ,
/// tự suy đoán nội dung theo chữ Hán/Pinyin nếu không có header, và ghi log chẩn đoán chi tiết.
/// </summary>
public static class HskVocabCsvParser
{
    public static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Phân tách chuỗi CSV thành các dòng và ô, hỗ trợ ngoặc kép và dấu phân cách tự động.
    /// </summary>
    public static IEnumerable<List<string>> Parse(string content)
    {
        if (string.IsNullOrEmpty(content)) return Enumerable.Empty<List<string>>();
        content = content.TrimStart('\uFEFF');

        char delimiter = DetectDelimiter(content);

        var rows = new List<List<string>>();
        var field = new StringBuilder();
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
                current.Add(field.ToString().Trim());
                field.Clear();
            }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n') i++;
                current.Add(field.ToString().Trim());
                field.Clear();
                if (current.Any(s => !string.IsNullOrWhiteSpace(s)))
                {
                    rows.Add(current);
                }
                current = new List<string>();
            }
            else field.Append(c);
        }

        if (field.Length > 0 || current.Count > 0)
        {
            current.Add(field.ToString().Trim());
            if (current.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                rows.Add(current);
            }
        }
        return rows;
    }

    /// <summary>
    /// Tự động dò tìm dấu phân cách phổ biến: phẩy (,), chấm phẩy (;), tab (\t), thanh đứng (|).
    /// </summary>
    public static char DetectDelimiter(string content)
    {
        int commas = 0, semicolons = 0, tabs = 0, pipes = 0;
        bool inQuotes = false;
        foreach (char c in content)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (!inQuotes)
            {
                if (c == ',') commas++;
                else if (c == ';') semicolons++;
                else if (c == '\t') tabs++;
                else if (c == '|') pipes++;
                else if (c == '\n') break; // chỉ xét dòng đầu tiên
            }
        }
        if (pipes > commas && pipes >= semicolons && pipes >= tabs) return '|';
        if (semicolons > commas && semicolons >= tabs) return ';';
        if (tabs > commas && tabs > semicolons) return '\t';
        return ',';
    }

    /// <summary>
    /// Xử lý các dòng dữ liệu thô (từ CSV hoặc Excel), tự nhận dạng cột và chuẩn hóa thành HskParsedRow kèm log chi tiết.
    /// </summary>
    public static (List<HskParsedRow> Rows, List<string> Logs) ProcessRows(
        List<List<string>> rawRows,
        string fileName,
        ILogger? logger = null)
    {
        var logs = new List<string>();
        void Log(string msg)
        {
            logs.Add(msg);
            logger?.LogInformation("{HskLog}", msg);
        }

        if (rawRows == null || rawRows.Count == 0)
        {
            Log($"[HSK Parser] File '{fileName}' không chứa dòng dữ liệu nào.");
            return (new List<HskParsedRow>(), logs);
        }

        Log($"[HSK Parser] Đọc file '{fileName}': tìm thấy {rawRows.Count} dòng dữ liệu.");

        // Suy đoán HskLevel từ tên file (ví dụ HSK1_TuVung.csv -> HSK1)
        string inferredLevel = "HSK1";
        var lowerFileName = fileName.ToLowerInvariant();
        for (int lvl = 1; lvl <= 9; lvl++)
        {
            if (lowerFileName.Contains($"hsk{lvl}") || lowerFileName.Contains($"hsk {lvl}") || lowerFileName.Contains($"hsk_{lvl}"))
            {
                inferredLevel = $"HSK{lvl}";
                break;
            }
        }

        // Kiểm tra dòng đầu tiên xem có phải là tiêu đề (header) không
        var firstRow = rawRows[0].Select(c => c?.Trim().Trim('\uFEFF') ?? "").ToList();
        var (isHeader, mapping) = DetectColumns(firstRow, rawRows);

        IEnumerable<List<string>> dataRows = rawRows;
        if (isHeader)
        {
            Log($"[HSK Parser] Dòng 1 được xác định là TIÊU ĐỀ CỘT: [{string.Join(" | ", firstRow)}]");
            dataRows = rawRows.Skip(1);
        }
        else
        {
            Log($"[HSK Parser] Dòng 1 là DỮ LIỆU. Đã dùng thuật toán quét Unicode để tự động suy đoán vị trí cột.");
        }

        string DescribeCol(int idx, string defaultVal) =>
            idx >= 0 && idx < firstRow.Count 
                ? $"Cột {idx + 1} (\"{firstRow[idx]}\")" 
                : defaultVal;

        Log($"[HSK Parser] Kết quả ánh xạ cột: " +
            $"HskLevel={DescribeCol(mapping.LevelCol, $"Mặc định '{inferredLevel}'")}, " +
            $"Hanzi={DescribeCol(mapping.HanziCol, "KHÔNG TÌM THẤY")}, " +
            $"Pinyin={DescribeCol(mapping.PinyinCol, "Trống")}, " +
            $"Meaning={DescribeCol(mapping.MeaningCol, "Trống")}, " +
            $"WordType={DescribeCol(mapping.WordTypeCol, "Trống")}, " +
            $"ExampleSentence={DescribeCol(mapping.ExampleCol, "Trống")}, " +
            $"Topic={DescribeCol(mapping.TopicCol, "Trống")}");

        if (mapping.HanziCol < 0)
        {
            Log($"[HSK Parser LỖI] Không thể tìm thấy cột Chữ Hán (Hanzi) trong file '{fileName}'. Hãy đảm bảo file có cột chứa chữ Hán hoặc có tiêu đề 'Hanzi' / 'Chữ Hán' / 'Từ vựng'.");
            return (new List<HskParsedRow>(), logs);
        }

        var result = new List<HskParsedRow>();
        int rowIndex = isHeader ? 2 : 1;
        int skippedCount = 0;

        foreach (var fields in dataRows)
        {
            if (fields.All(string.IsNullOrWhiteSpace))
            {
                rowIndex++;
                continue;
            }

            string Get(int idx) => idx >= 0 && idx < fields.Count ? fields[idx]?.Trim() ?? "" : "";

            var hanziVal = Get(mapping.HanziCol);
            if (string.IsNullOrWhiteSpace(hanziVal))
            {
                skippedCount++;
                rowIndex++;
                continue;
            }

            // HskLevel: nếu cột rỗng thì dùng inferredLevel
            var levelVal = Get(mapping.LevelCol);
            if (string.IsNullOrWhiteSpace(levelVal))
            {
                levelVal = inferredLevel;
            }
            else if (!levelVal.StartsWith("HSK", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(levelVal, out int lvlNum) && lvlNum >= 1 && lvlNum <= 9)
                    levelVal = $"HSK{lvlNum}";
                else
                    levelVal = inferredLevel;
            }
            levelVal = levelVal.ToUpperInvariant();

            var pinyinVal = Get(mapping.PinyinCol);
            var meaningVal = Get(mapping.MeaningCol);
            var wordTypeVal = NullIfEmpty(Get(mapping.WordTypeCol));
            var exampleSentence = NullIfEmpty(Get(mapping.ExampleCol));
            var examplePinyin = NullIfEmpty(Get(mapping.ExamplePinyinCol));
            var exampleMeaning = NullIfEmpty(Get(mapping.ExampleMeaningCol));
            var audioUrl = NullIfEmpty(Get(mapping.AudioCol));
            int orderVal = int.TryParse(Get(mapping.OrderCol), out int ov) ? ov : result.Count + 1;
            var topicVal = NullIfEmpty(Get(mapping.TopicCol));

            result.Add(new HskParsedRow(
                levelVal,
                hanziVal,
                pinyinVal,
                meaningVal,
                wordTypeVal,
                exampleSentence,
                examplePinyin,
                exampleMeaning,
                audioUrl,
                orderVal,
                topicVal
            ));

            rowIndex++;
        }

        Log($"[HSK Parser] Trích xuất thành công {result.Count} dòng từ vựng hợp lệ (bỏ qua {skippedCount} dòng trống hoặc không có chữ Hán).");
        if (result.Count > 0)
        {
            var first = result[0];
            Log($"[HSK Parser] Dòng mẫu đầu tiên: Cấp='{first.Level}', Chữ Hán='{first.Hanzi}', Pinyin='{first.Pinyin}', Nghĩa='{first.Meaning}', Chủ đề='{first.Topic ?? "Mặc định"}'");
        }

        return (result, logs);
    }

    /// <summary>
    /// Tự nhận diện cấu trúc cột theo tiêu đề hoặc theo dữ liệu mẫu Unicode.
    /// </summary>
    private static (bool IsHeader, ColumnMapping Mapping) DetectColumns(
        List<string> firstRow,
        List<List<string>> allRows)
    {
        var mapping = new ColumnMapping();
        var cleanHeaders = firstRow.Select(h => Regex.Replace(h.ToLowerInvariant(), @"[\s\-_]", "")).ToList();

        // Danh sách từ khóa nhận diện dòng header
        string[] headerSignatures = {
            "hsk", "level", "cấp", "cap", "hanzi", "chữhán", "chuhan", "từvựng", "tuvung", "từ", "tu",
            "word", "chinese", "pinyin", "bínhâm", "binham", "phiênâm", "phienam",
            "nghĩa", "nghia", "meaning", "dịch", "dich", "giảithích", "loạitừ", "loaitu", "vídụ", "vidu", "stt"
        };

        bool hasHeaderSignature = cleanHeaders.Any(h => headerSignatures.Any(sig => h == sig || h.Contains(sig)));

        if (hasHeaderSignature)
        {
            var usedCols = new HashSet<int>();

            int Match(params string[] aliases)
            {
                // Bước 1: Khớp chính xác hoàn toàn (Exact match)
                for (int i = 0; i < cleanHeaders.Count; i++)
                {
                    if (usedCols.Contains(i)) continue;
                    var h = cleanHeaders[i];
                    foreach (var a in aliases)
                    {
                        var cleanA = Regex.Replace(a.ToLowerInvariant(), @"[\s\-_]", "");
                        if (h == cleanA)
                        {
                            usedCols.Add(i);
                            return i;
                        }
                    }
                }
                // Bước 2: Khớp chứa (Contains) với từ khóa có độ dài >= 2 ký tự
                for (int i = 0; i < cleanHeaders.Count; i++)
                {
                    if (usedCols.Contains(i)) continue;
                    var h = cleanHeaders[i];
                    foreach (var a in aliases)
                    {
                        var cleanA = Regex.Replace(a.ToLowerInvariant(), @"[\s\-_]", "");
                        if (cleanA.Length >= 2 && h.Contains(cleanA))
                        {
                            usedCols.Add(i);
                            return i;
                        }
                    }
                }
                return -1;
            }

            mapping.LevelCol = Match("hsklevel", "hsk_level", "hsk level", "level", "cấpđộ", "capdo", "cấp", "cap", "hsk");
            mapping.WordTypeCol = Match("wordtype", "word type", "loạitừ", "loaitu", "từloại", "tuloai", "partofspeech", "part of speech", "pos", "type");
            mapping.HanziCol = Match("hanzi", "chữhán", "chuhan", "chữhángiảnthể", "giảnthể", "từvựng", "tuvung", "từ", "tu", "word", "simplified", "chinese", "character", "vocab", "tiếngtrung");
            mapping.PinyinCol = Match("pinyin", "bínhâm", "binham", "phiênâm", "phienam", "reading", "pronunciation", "âmđọc");
            mapping.MeaningCol = Match("meaning", "nghĩatiếngviệt", "nghiatiengviet", "dịchnghĩa", "dichnghia", "nghĩa", "nghia", "dịch", "dich", "giảithích", "giaithich", "địnhnghĩa", "vietnamese", "tiếngviệt", "definition", "translation", "ýnghĩa");
            mapping.ExampleCol = Match("examplesentence", "example sentence", "câuvídụ", "cauvidu", "câumẫu", "vídụ", "vidu", "sentence", "example");
            mapping.ExamplePinyinCol = Match("examplepinyin", "example pinyin", "pinyinvídụ", "pinyin_vidu", "pinyincâuvídụ");
            mapping.ExampleMeaningCol = Match("examplemeaning", "example meaning", "nghĩavídụ", "dịchvídụ", "dịchcâuvídụ", "nghĩacâuvídụ");
            mapping.TopicCol = Match("topic", "chủđề", "chude", "chủđiểm", "chudiem");
            mapping.AudioCol = Match("audiourl", "audio url", "audio", "phátâm", "sound", "mp3");
            mapping.OrderCol = Match("displayorder", "order", "stt", "thứtự", "thutu", "id", "no");

            if (mapping.HanziCol >= 0)
            {
                return (true, mapping);
            }
        }

        // Bước 3 Heuristic fallback: Quét mẫu các dòng đầu tiên để tự nhận diện cột qua Unicode và Tone marks
        var sampleRows = allRows.Take(20).ToList();
        int colCount = sampleRows.Max(r => r.Count);
        int hanziBestCol = -1, hanziBestScore = 0;
        int pinyinBestCol = -1, pinyinBestScore = 0;

        for (int c = 0; c < colCount; c++)
        {
            int chineseCount = 0;
            int pinyinCount = 0;

            foreach (var r in sampleRows)
            {
                if (c < r.Count && !string.IsNullOrWhiteSpace(r[c]))
                {
                    var text = r[c].Trim();
                    if (Regex.IsMatch(text, @"[\u4e00-\u9fa5]")) chineseCount++;
                    if (Regex.IsMatch(text, @"[āáǎàēéěèīíǐìōóǒòūúǔùǖǘǚǜ]")) pinyinCount++;
                }
            }

            if (chineseCount > hanziBestScore)
            {
                hanziBestScore = chineseCount;
                hanziBestCol = c;
            }
            if (pinyinCount > pinyinBestScore)
            {
                pinyinBestScore = pinyinCount;
                pinyinBestCol = c;
            }
        }

        mapping.HanziCol = hanziBestCol;
        mapping.PinyinCol = pinyinBestCol;

        // Dò cột HSK Level và Nghĩa (Meaning)
        for (int c = 0; c < colCount; c++)
        {
            if (c != mapping.HanziCol && c != mapping.PinyinCol)
            {
                bool isLevelCol = sampleRows.Any(r => c < r.Count && Regex.IsMatch(r[c].Trim(), @"^(hsk\s*[1-9]|[1-9])$", RegexOptions.IgnoreCase));
                if (isLevelCol && mapping.LevelCol < 0)
                {
                    mapping.LevelCol = c;
                    continue;
                }
                if (mapping.MeaningCol < 0)
                {
                    mapping.MeaningCol = c;
                }
                else if (mapping.ExampleCol < 0)
                {
                    mapping.ExampleCol = c;
                }
            }
        }

        return (false, mapping);
    }
}
