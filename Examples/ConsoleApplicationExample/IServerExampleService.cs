using GenericRpc;

public interface IServerExampleService
{
    string Concat(string text1, string text2);
    void ShowMessage(string message);
}

internal class ServerExampleService : IServerExampleService
{
    private readonly ClientContext _context;

    public ServerExampleService(ClientContext context)
    {
        _context = context;
    }

    public string Concat(string text1, string text2)
    {
        var result = string.Concat(text1, text2);
        Console.WriteLine($"Concat for `{_context.Id:N}`: `{result}`");
        return result;
    }

    public void ShowMessage(string message) => Console.WriteLine($"Message from `{_context.Id:N}`: `{message}`");
}
