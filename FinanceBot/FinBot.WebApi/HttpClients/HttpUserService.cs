using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Requests;
using FinBot.Domain.Utils;

namespace FinBot.WebApi.HttpClients;

public class HttpUserService(HttpClient httpClient) : BaseHttpService(httpClient, "/Users"), IUserService
{
    public Task<Result<User>> CreateUserAsync(
        long tgId,
        string displayName) =>
        PostAsync<User, CreateUserRequest>("/", new CreateUserRequest(tgId, displayName));

    public Task<Result<User>> GetOrCreateUserAsync(
        long tgId,
        string displayName) =>
        PostAsync<User, CreateUserRequest>("/ensure", new CreateUserRequest(tgId, displayName));

    public Task<Result<User>> GetUserByGuidIdAsync(Guid userId) =>
        GetAsync<User>($"/{userId}");

    public Task<Result<User>> GetUserByTgIdAsync(long userId) =>
        GetAsync<User>($"/{userId}");
}