using BankingApi.Application.Dto;
using BankingApi.Application.Interfaces;
using BankingApi.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Services;

public class CategoryService(AppDbContext context) : ICategoryService
{
    public async Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Categories
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type.ToString()))
            .ToListAsync(ct);
    }
}