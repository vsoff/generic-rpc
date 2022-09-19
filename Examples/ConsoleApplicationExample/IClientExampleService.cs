using GenericRpc;

public interface IClientExampleService
{
    void ShowMessage(string message);
}

internal class ClientExampleService : ListenerService, IClientExampleService
{
    public void ShowMessage(string message) => Console.WriteLine($"Message from server: `{message}`");
}
