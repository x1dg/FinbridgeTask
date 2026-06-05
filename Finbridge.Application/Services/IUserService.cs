using Finbridge.Application.Contracts;

namespace Finbridge.Application.Services;

/// <summary>
/// Application Service для работы с пользователями.
/// Тонкий слой над репозиторием и агрегатом User — оркестрирует use-case'ы
/// и маппит доменные сущности в DTO ответа.
/// </summary>
public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
