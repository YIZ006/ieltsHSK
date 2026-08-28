using Backend.Application.DTOs;

namespace Backend.Application.Abstractions;

public interface IAiGradingService
{
    Task<GradeWritingResponse> GradeWritingAsync(GradeWritingRequest request, CancellationToken cancellationToken = default);
    Task<GradeSpeakingResponse> GradeSpeakingAsync(GradeSpeakingRequest request, CancellationToken cancellationToken = default);
}
