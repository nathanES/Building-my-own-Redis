using System.Net;
using System.Net.Sockets;
using System.Security.Authentication.ExtendedProtection;
using codecrafters_redis;
using codecrafters_redis.Commands;
using codecrafters_redis.DependencyInjection;
using codecrafters_redis.Protocol;
using codecrafters_redis.RedisRepositories.Configuration;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    private static RedisCommandsRegistry _redisCommandsRegistry = null!;

    static async Task Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddDependencies()
            .BuildServiceProvider();

        _redisCommandsRegistry = new RedisCommandsRegistry(serviceProvider);
        //Load configurations
        await ConfigurationLoader.LoadConfiguration(
            serviceProvider.GetRequiredService<IRedisConfigRepository>(),
            Environment.GetCommandLineArgs().Skip(1).ToArray());

        var cts = serviceProvider.GetRequiredService<CancellationTokenSource>();
        Console.CancelKeyPress += (sender, args) =>
        {
            Console.WriteLine("Shutdown signal received...");
            args.Cancel = true;
            cts.Cancel();
        };
        Console.WriteLine("Starting server...");
        TcpListener server = new TcpListener(IPAddress.Any, 6379);
        server.Start();
        Console.WriteLine("Server started on port 6379");
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var socket = await server.AcceptSocketAsync(cts.Token);
                Console.WriteLine("Client connected");
                _ = Task.Run(() => HandleClient(socket, cts.Token), cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Server shutting down...");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error : {e.Message}");
        }
        finally
        {
            server.Stop();
            Console.WriteLine("Server stopped");
        }
    }

    private static async Task HandleClient(Socket socket, CancellationToken ct)
    {
        try
        {
            string clientId = Guid.NewGuid().ToString();
            while (socket.Connected) // Client disconnects normally → socket.ReceiveAsync() returns 0, closing the loop.
            {
                var buffer = new byte[1_024];
                var requestLength = await socket.ReceiveAsync(buffer);
                Console.WriteLine($"Received {requestLength} bytes");
                var respRequest = RespRequest.Parse(buffer[..requestLength], requestLength);
                if (respRequest == null)
                {
                    Console.WriteLine("Received null request");
                    return;
                }

                
                var respResponse = await HandleRequest(respRequest, clientId);
                await SendResponse(socket, respResponse, ct);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Client error: {e.Message}");
        }
        finally
        {
            if (socket.Connected)
            {
                Console.WriteLine("Shutting Down the Socket connection...");
                socket.Shutdown(SocketShutdown.Both);
                Console.WriteLine("Socket connection is shut down");
            }

            Console.WriteLine("Closing the Socket connection...");
            socket.Close();
            Console.WriteLine("Socket connection closed");
        }
    }

    private static Task<RespResponse> HandleRequest(RespRequest request, string clientId)
    {
        var redisCommandHandler = _redisCommandsRegistry.GetHandler(request.Command);
        return redisCommandHandler.HandleAsync(clientId, request);
    }

    private static async Task SendResponse(Socket socket, RespResponse respResponse, CancellationToken ct = default)
    {
        await socket.SendAsync(respResponse.GetRawResponse(), cancellationToken: ct);
    }
}