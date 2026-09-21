using System.Net;

namespace PartnerIntegration.UnitTests.Fakes;

public sealed class ScriptedHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();

    public IReadOnlyList<HttpRequestMessage> Requests => _requests;

    public int CallCount => _requests.Count;

    public ScriptedHttpMessageHandler Succeed(HttpStatusCode statusCode, string json)
    {
        _responses.Enqueue(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        return this;
    }

    public ScriptedHttpMessageHandler Fail(HttpStatusCode statusCode, string? json = null)
    {
        _responses.Enqueue(_ => new HttpResponseMessage(statusCode)
        {
            Content = json is null ? null : new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        return this;
    }

    public ScriptedHttpMessageHandler Throw(Exception exception)
    {
        _responses.Enqueue(_ => throw exception);
        return this;
    }

    public ScriptedHttpMessageHandler Repeat(int count, Func<HttpRequestMessage, HttpResponseMessage> factory)
    {
        for (var i = 0; i < count; i++)
        {
            _responses.Enqueue(factory);
        }

        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _requests.Add(request);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No scripted HTTP response remains.");
        }

        var response = _responses.Dequeue()(request);
        return Task.FromResult(response);
    }
}
