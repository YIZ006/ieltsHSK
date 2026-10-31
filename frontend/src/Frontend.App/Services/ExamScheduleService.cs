using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Frontend.App.Services;

public class ExamScheduleItem
{
    public string Id { get; set; } = string.Empty;
    public string CertificateType { get; set; } = "HSK"; // "HSK" or "IELTS"
    public string ExamType { get; set; } = "HSK"; // "HSK", "IELTS_IDP", "IELTS_BC"
    public string? Level { get; set; } // "HSK 3 - 6 & HSKK", "Academic / General Training"
    public DateTime ExamDate { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public string TestCenter { get; set; } = string.Empty;
    public string? LocationAddress { get; set; }
    public string City { get; set; } = "Hà Nội";
    public string Format { get; set; } = "Computer"; // "Computer" or "Paper"
    public decimal FeeVnd { get; set; }
    public string? FeeFormatted { get; set; }
    public string Organizer { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RegistrationUrl { get; set; }
    public string Status { get; set; } = "RegistrationOpen"; // "Open", "ClosingSoon", "Closed", "RegistrationOpen"
    public string? Notes { get; set; }

    [JsonIgnore]
    public string EffectiveStatus
    {
        get
        {
            var today = DateTime.Today;
            if (string.Equals(Status, "Closed", StringComparison.OrdinalIgnoreCase)) return "Closed";
            if (RegistrationDeadline != default && RegistrationDeadline.Date < today) return "Closed";
            if (ExamDate != default && ExamDate.Date < today) return "Closed";
            if (RegistrationDeadline != default)
            {
                var days = (RegistrationDeadline.Date - today).TotalDays;
                if (days >= 0 && days <= 7) return "ClosingSoon";
            }
            if (string.Equals(Status, "ClosingSoon", StringComparison.OrdinalIgnoreCase)) return "ClosingSoon";
            return "Open";
        }
    }

    [JsonIgnore]
    public bool IsClosed => string.Equals(EffectiveStatus, "Closed", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsClosingSoon => string.Equals(EffectiveStatus, "ClosingSoon", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsOpen => string.Equals(EffectiveStatus, "Open", StringComparison.OrdinalIgnoreCase);
}

public class ExamFeeItem
{
    public string LevelOrType { get; set; } = string.Empty;
    public decimal FeeVnd { get; set; }
    public string FormattedVnd { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class ExamFeeMatrix
{
    public List<ExamFeeItem> HskFees { get; set; } = new();
    public List<ExamFeeItem> IeltsFees { get; set; } = new();
}

public class TestCenterLocation
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExamType { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Hotline { get; set; }
    public string? Website { get; set; }
}

public class ExamRegulations
{
    public List<string> RequiredDocuments { get; set; } = new();
    public List<string> PermittedItems { get; set; } = new();
    public List<string> ProhibitedItems { get; set; } = new();
    public List<string> TimelineTips { get; set; } = new();
}

public class ExamScheduleCatalogRoot
{
    public string AcademicYear { get; set; } = "2026";
    public string? LastUpdated { get; set; }
    public int TotalSchedules { get; set; }
    public List<ExamScheduleItem> Schedules { get; set; } = new();
    public ExamFeeMatrix? FeeMatrix { get; set; }
    public List<TestCenterLocation> Locations { get; set; } = new();
    public ExamRegulations? Regulations { get; set; }
}

// DTO for backend deserialization if returned as int ID
internal class BackendExamScheduleDto
{
    public int Id { get; set; }
    public string CertificateType { get; set; } = string.Empty;
    public string? Level { get; set; }
    public DateTime ExamDate { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public string Location { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Format { get; set; } = "Computer";
    public decimal FeeVnd { get; set; }
    public string Organizer { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RegistrationUrl { get; set; }
    public string Status { get; set; } = "RegistrationOpen";
    public bool IsActive { get; set; } = true;
}

public class ExamScheduleService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private ExamScheduleCatalogRoot? _catalogCache;

    public ExamScheduleService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    private string GetApiUrl(string path)
    {
        var baseUrl = _configuration["BackendApi:BaseUrl"] ?? "http://localhost:5101/";
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    public async Task<List<ExamScheduleItem>> GetSchedulesAsync(
        string? certificateType = null,
        string? city = null,
        string? format = null,
        string? search = null,
        bool forceRefresh = false)
    {
        // 1. Try Live Backend API First
        try
        {
            var queryParams = new List<string>();
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            queryParams.Add($"_t={ts}");

            if (!string.IsNullOrWhiteSpace(certificateType) && certificateType != "All" && certificateType != "Tất cả")
                queryParams.Add($"certificateType={Uri.EscapeDataString(certificateType)}");
            if (!string.IsNullOrWhiteSpace(city) && city != "All" && city != "Tất cả")
                queryParams.Add($"city={Uri.EscapeDataString(city)}");
            if (!string.IsNullOrWhiteSpace(format) && format != "All" && format != "Tất cả")
                queryParams.Add($"format={Uri.EscapeDataString(format)}");
            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            var url = GetApiUrl($"api/exam-schedules?{string.Join("&", queryParams)}");
            var apiList = await _http.GetFromJsonAsync<List<BackendExamScheduleDto>>(url);
            if (apiList != null && apiList.Count > 0)
            {
                return apiList.Select(x => new ExamScheduleItem
                {
                    Id = x.Id.ToString(),
                    CertificateType = x.CertificateType,
                    ExamType = x.CertificateType.StartsWith("IELTS", StringComparison.OrdinalIgnoreCase) ? (x.Organizer.Contains("IDP") ? "IELTS_IDP" : "IELTS_BC") : "HSK",
                    Level = x.Level,
                    ExamDate = x.ExamDate,
                    RegistrationDeadline = x.RegistrationDeadline,
                    TestCenter = x.Location,
                    LocationAddress = x.Location,
                    City = x.City,
                    Format = x.Format,
                    FeeVnd = x.FeeVnd,
                    FeeFormatted = $"{x.FeeVnd:N0} VNĐ",
                    Organizer = x.Organizer,
                    Description = x.Description,
                    RegistrationUrl = x.RegistrationUrl,
                    Status = x.Status
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExamScheduleService] API query failed: {ex.Message}. Falling back to sample JSON.");
        }

        // 2. Fallback to sample-data/exam-schedules-2026.json
        var catalog = await GetCatalogAsync(forceRefresh);
        if (catalog?.Schedules == null || catalog.Schedules.Count == 0)
            return new List<ExamScheduleItem>();

        var filtered = catalog.Schedules.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(certificateType) && certificateType != "All" && certificateType != "Tất cả")
        {
            var certNorm = certificateType.Trim().ToUpperInvariant();
            filtered = filtered.Where(s => s.CertificateType.ToUpperInvariant().Contains(certNorm) || s.ExamType.ToUpperInvariant().Contains(certNorm));
        }

        if (!string.IsNullOrWhiteSpace(city) && city != "All" && city != "Tất cả" && city != "Toàn quốc")
        {
            var cityNorm = city.Trim().ToLowerInvariant();
            filtered = filtered.Where(s => s.City.ToLowerInvariant().Contains(cityNorm));
        }

        if (!string.IsNullOrWhiteSpace(format) && format != "All" && format != "Tất cả")
        {
            var formatNorm = format.Trim().ToLowerInvariant();
            filtered = filtered.Where(s => s.Format.ToLowerInvariant().Contains(formatNorm));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            filtered = filtered.Where(item =>
                item.TestCenter.ToLowerInvariant().Contains(s) ||
                item.City.ToLowerInvariant().Contains(s) ||
                item.Organizer.ToLowerInvariant().Contains(s) ||
                (item.Level != null && item.Level.ToLowerInvariant().Contains(s)) ||
                (item.Description != null && item.Description.ToLowerInvariant().Contains(s)));
        }

        return filtered.OrderBy(s => s.ExamDate).ToList();
    }

    public async Task<ExamScheduleCatalogRoot?> GetCatalogAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _catalogCache != null)
            return _catalogCache;

        try
        {
            var root = await _http.GetFromJsonAsync<ExamScheduleCatalogRoot>("sample-data/exam-schedules-2026.json");
            if (root != null)
            {
                _catalogCache = root;
            }
            return _catalogCache;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExamScheduleService] Failed to load sample-data/exam-schedules-2026.json: {ex.Message}");
            return _catalogCache ?? new ExamScheduleCatalogRoot();
        }
    }

    public async Task<ExamFeeMatrix> GetFeeMatrixAsync()
    {
        var catalog = await GetCatalogAsync();
        return catalog?.FeeMatrix ?? new ExamFeeMatrix();
    }

    public async Task<List<TestCenterLocation>> GetLocationsAsync()
    {
        var catalog = await GetCatalogAsync();
        return catalog?.Locations ?? new List<TestCenterLocation>();
    }

    public async Task<ExamRegulations> GetRegulationsAsync()
    {
        var catalog = await GetCatalogAsync();
        return catalog?.Regulations ?? new ExamRegulations();
    }
}
