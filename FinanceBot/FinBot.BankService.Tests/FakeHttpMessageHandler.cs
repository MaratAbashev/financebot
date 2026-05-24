namespace FinBot.BankService.Tests;

public class FakeHttpMessageHandler(System.Net.HttpStatusCode statusCode, string content)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        });
    }
}