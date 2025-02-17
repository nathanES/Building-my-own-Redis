using System.Net;
using System.Net.Sockets;
using System.Security.Authentication.ExtendedProtection;
using codecrafters_redis;
using codecrafters_redis.RedisCommands;
using codecrafters_redis.RespRequestResponse;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    private static RedisCommandsRegistry _redisCommandsRegistry;

    static async Task Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddDependencies()
            .BuildServiceProvider();
        
        _redisCommandsRegistry = new RedisCommandsRegistry(serviceProvider);

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

                var respResponse = HandleRequest(respRequest);
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

    private static RespResponse HandleRequest(RespRequest request)
    {
        var redisCommandHandler = _redisCommandsRegistry.GetHandler(request.Command);
        return redisCommandHandler.Handle(request);
    }

    private static async Task SendResponse(Socket socket, RespResponse respResponse, CancellationToken ct = default)
    {
        await socket.SendAsync(respResponse.GetRawResponse(), cancellationToken: ct);
    }
}