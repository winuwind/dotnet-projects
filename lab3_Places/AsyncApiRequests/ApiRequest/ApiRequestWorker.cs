namespace AsyncApiRequests.ApiRequest;

public static class ApiRequestWorker
{
    private static readonly HttpClient Сlient = new HttpClient();
    
    static ApiRequestWorker()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            Сlient.Dispose();
        };
    }
    
    public static Task<HttpResponseMessage> Send(HttpRequestMessage msg)
    {
        return Сlient.SendAsync(msg);
    }
}