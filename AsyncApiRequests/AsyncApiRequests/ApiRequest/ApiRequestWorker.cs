namespace AsyncApiRequests.ApiRequest;

public static class ApiRequestWorker
{
    public static async Task<HttpResponseMessage> Send(HttpRequestMessage msg)
    {
        using var client = new HttpClient();
        return await client.SendAsync(msg);
    }
}