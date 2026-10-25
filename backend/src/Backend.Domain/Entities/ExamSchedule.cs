namespace Backend.Domain.Entities;

public class ExamSchedule
{
    public int Id { get; set; }

    /// <summary>
    /// Chứng chỉ: "HSK", "HSKK", "IELTS"
    /// </summary>
    public required string CertificateType { get; set; }

    /// <summary>
    /// Cấp độ / Hệ thi: vd "HSK 3-6 & HSKK", "HSK 1-2", "Academic / General Training"
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// Ngày thi chính thức
    /// </summary>
    public DateTime ExamDate { get; set; }

    /// <summary>
    /// Hạn chót đăng ký nộp hồ sơ
    /// </summary>
    public DateTime RegistrationDeadline { get; set; }

    /// <summary>
    /// Địa điểm / Điểm thi cụ thể (vd: "Viện Khổng Tử - HANU", "IDP Hai Bà Trưng")
    /// </summary>
    public required string Location { get; set; }

    /// <summary>
    /// Tỉnh / Thành phố: "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Huế", "Thái Nguyên"...
    /// </summary>
    public required string City { get; set; }

    /// <summary>
    /// Hình thức thi: "Computer" (Trên máy tính), "Paper" (Trên giấy)
    /// </summary>
    public string Format { get; set; } = "Computer";

    /// <summary>
    /// Lệ phí thi (VND)
    /// </summary>
    public decimal FeeVnd { get; set; }

    /// <summary>
    /// Đơn vị tổ chức: "IDP", "British Council", "Chinese Testing International (CTI)"...
    /// </summary>
    public required string Organizer { get; set; }

    /// <summary>
    /// Mô tả bổ sung: Giờ thi, lưu ý phòng thi, quy chế
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Đường dẫn trực tiếp đến cổng đăng ký chính thức
    /// </summary>
    public string? RegistrationUrl { get; set; }

    /// <summary>
    /// Trạng thái đăng ký: "RegistrationOpen", "RegistrationClosingSoon", "RegistrationClosed"
    /// </summary>
    public string Status { get; set; } = "RegistrationOpen";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
