using System.Net;
using System.Net.Sockets;
using System.Security.Authentication.ExtendedProtection;
using codecrafters_redis;
using codecrafters_redis.Commands;
using codecrafters_redis.DependencyInjection;
using codecrafters_redis.Protocol;
using codecrafters_redis.RedisRepositories.Configuration;
using codecrafters_redis.RedisRepositories.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

class Program
{
    private static RedisCommandsRegistry _redisCommandsRegistry = null!;

    static async Task Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddDependencies()
            .BuildServiceProvider();

        var cts = serviceProvider.GetRequiredService<CancellationTokenSource>();

        //Load requirements
        var host = serviceProvider.GetRequiredService<IHostedService>();
        await host.StartAsync(cts.Token); 

        await serviceProvider.GetRequiredService<IRedisStorageRepository>().LoadConfigurationAsync();
        
        //Register redis command
        _redisCommandsRegistry = new RedisCommandsRegistry(serviceProvider);

        Console.CancelKeyPress += (sender, args) =>
        {
            Console.WriteLine("[Debug] - Shutdown signal received...");
            args.Cancel = true;
            cts.Cancel();
        };
        var configurationRepository = serviceProvider.GetRequiredService<IRedisConfigRepository>();
        Console.WriteLine("[Debug] - Starting server...");
        var server = new TcpListener(IPAddress.Any, int.Parse(await configurationRepository.GetAsync("port") ?? "6379"));
        server.Start();
        Console.WriteLine("[Debug] - Server started on port");
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var socket = await server.AcceptSocketAsync(cts.Token);
                Console.WriteLine("[Debug] - Client connected");
                _ = Task.Run(() => HandleClient(socket, cts.Token), cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[Debug] - Server shutting down...");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Error] - Error : {e.Message}");
        }
        finally
        {
            server.Stop();
            Console.WriteLine("[Debug] - Server stopped");
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
                Console.WriteLine($"[Debug] - Received {requestLength} bytes");
                var respRequest = RespRequest.Parse(buffer[..requestLength], requestLength);
                if (respRequest == null)
                {
                    Console.WriteLine("[Debug] - Received null request");
                    return;
                }

                
                var respResponse = await HandleRequest(respRequest, clientId);
                await SendResponse(socket, respResponse, ct);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Error] - Client error: {e.Message}");
        }
        finally
        {
            if (socket.Connected)
            {
                Console.WriteLine("[Debug] - Shutting Down the Socket connection...");
                socket.Shutdown(SocketShutdown.Both);
                Console.WriteLine("[Debug] - Socket connection is shut down");
            }

            Console.WriteLine("[Debug] - Closing the Socket connection...");
            socket.Close();
            Console.WriteLine("[Debug] - Socket connection closed");
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