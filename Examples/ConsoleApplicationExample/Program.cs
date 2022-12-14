using GenericRpc;
using GenericRpc.Communicators;
using GenericRpc.Serialization;
using GenericRpc.SocketTransport;
using GenericRpc.SocketTransport.Common;
using GenericRpc.Transport;
using System.Collections.Concurrent;

try
{
    bool isServer = false;
    while (true)
    {
        Console.WriteLine("Write `client` or `server`:");
        var input = Console.ReadLine();

        if (input == "client" || input == "c")
        {
            break;
        }

        if (input == "server" || input == "s")
        {
            isServer = true;
            break;
        }
    }

    const string ip = "127.0.0.1";
    const int port = 51337;

    if (isServer)
    {
        IServerTransportLayer transportLayer = new ServerSocketTransportLayer();
        var serverCommunicator = new CommunicatorBuilder()
                        .SetSerializer(new DefaultCommunicatorSerializer())
                        .SetServerTransportLayer(transportLayer)
                        .RegisterListenerService<IServerExampleService, ServerExampleService>()
                        .RegisterProxyService<IClientExampleService>()
                        .BuildServer();

        await serverCommunicator.StartAsync(ip, port);

        var serverCancellactionTokenSource = new CancellationTokenSource();
        var serverCancellationToken = serverCancellactionTokenSource.Token;
        var tokensByContext = new ConcurrentDictionary<ClientContext, CancellationTokenSource>();

        transportLayer.OnClientConnected += async (context) =>
        {
            var service = serverCommunicator.GetProxy<IClientExampleService>(context);
            await Task.Factory.StartNew(async () =>
            {
                var clientTokenSource = new CancellationTokenSource();
                var clientToken = clientTokenSource.Token;
                tokensByContext.TryAdd(context, clientTokenSource);

                while (true)
                {
                    try
                    {
                        clientToken.ThrowIfCancellationRequested();
                        serverCancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(1000);

                        service.ShowMessage("Hello, I'm server!");
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine($"Sending messages cancelled");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error while executing methods: {ex}");
                    }
                }
            });
        };

        transportLayer.OnClientDisconnected += (context) =>
        {
            if (tokensByContext.TryRemove(context, out var source))
                source.Cancel();
        };

        Console.WriteLine("Press enter to stop server...");
        Console.ReadLine();

        serverCancellactionTokenSource.Cancel();
        await serverCommunicator.StopAsync();
        Console.WriteLine("Server stopped.");
    }
    else
    {
        var clientCommunicator = new CommunicatorBuilder()
            .SetSerializer(new DefaultCommunicatorSerializer())
            .SetClientTransportLayer(new ClientSocketTransportLayer())
            .RegisterProxyService<IServerExampleService>()
            .RegisterListenerService<IClientExampleService, ClientExampleService>()
            .BuildClient();

        await clientCommunicator.ConnectAsync(ip, port);
        var service = clientCommunicator.GetProxy<IServerExampleService>();

        var cancellactionTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellactionTokenSource.Token;
        await Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(1000);

                    var text = service.Concat("Hello ", "server!");
                    service.ShowMessage(text);
                }
                catch (GenericRpcSocketOfflineException)
                {
                    Console.WriteLine($"Socket is offline");
                    return;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Sending messages cancelled");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while executing methods: {ex}");
                }
            }
        });

        Console.WriteLine("Press enter to stop client...");
        Console.ReadLine();
        cancellactionTokenSource.Cancel();
        await clientCommunicator.DisconnectAsync();
        Console.WriteLine("Client stopped.");
    }

}
catch (Exception ex)
{
    Console.WriteLine($"Unhandled exception: {ex}");
}
finally
{
    Console.WriteLine("Press enter to close console...");
    Console.ReadLine();
}