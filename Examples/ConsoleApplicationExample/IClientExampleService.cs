using GenericRpc;

public interface IClientExampleService
{
    void ShowMessage(string message);
}

internal class ClientExampleService : IClientExampleService
{
    private readonly ClientContext _context;

    public ClientExampleService(ClientContext context)
    {
        _context = context;
    }

    public void ShowMessage(string message) => Console.WriteLine($"Message from server: `{message}`");
}
