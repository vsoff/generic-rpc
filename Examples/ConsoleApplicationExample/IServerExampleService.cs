using GenericRpc;

public interface IServerExampleService
{
    string Concat(string text1, string text2);
    void ShowMessage(string message);
}

internal class ServerExampleService : ListenerService, IServerExampleService
{
    public string Concat(string text1, string text2)
    {
        var result = string.Concat(text1, text2);
        Console.WriteLine($"Concat for `{ClientContext.Id:N}`: `{result}`");
        return result;
    }

    public void ShowMessage(string message) => Console.WriteLine($"Message from `{ClientContext.Id:N}`: `{message}`");
}
