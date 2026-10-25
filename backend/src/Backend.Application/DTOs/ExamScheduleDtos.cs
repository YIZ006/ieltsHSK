namespace Backend.Application.DTOs;

public class ExamScheduleDto
{
    public int Id { get; set; }
    public string CertificateType { get; set; } = string.Empty;
    public string? Level { get; set; }
    public DateTime ExamDate { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public string Location { get; set; } = string.Empty;
    public string TestCenter => Location;
    public string City { get; set; } = string.Empty;
    public string Format { get; set; } = "Computer";
    public decimal FeeVnd { get; set; }
    public string Organizer { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RegistrationUrl { get; set; }
    public string Status { get; set; } = "RegistrationOpen";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateExamScheduleRequest
{
    public string CertificateType { get; set; } = string.Empty;
    public string? Level { get; set; }
    public DateTime ExamDate { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? TestCenter { get; set; }
    public string City { get; set; } = string.Empty;
    public string Format { get; set; } = "Computer";
    public decimal FeeVnd { get; set; }
    public string Organizer { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RegistrationUrl { get; set; }
    public string? Status { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateExamScheduleRequest : CreateExamScheduleRequest
{
}

public class ExamScheduleFiltersDto
{
    public List<string> CertificateTypes { get; set; } = new();
    public List<string> Cities { get; set; } = new();
    public List<string> Formats { get; set; } = new();
    public List<string> Organizers { get; set; } = new();
}

public class ImportExamScheduleJsonRequest
{
    public string JsonContent { get; set; } = string.Empty;
}
