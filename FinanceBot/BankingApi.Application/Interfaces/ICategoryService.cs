using BankingApi.Application.Dto;

namespace BankingApi.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken ct = default);
}