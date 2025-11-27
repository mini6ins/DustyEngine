using System.IO.Pipes;
using System.Reflection;
using DustyEngine.Runner;

namespace StreamJsonRpc.Server
{
    internal class Server
    {
        private static RenderServer? _renderServer;

        private static void Main(string[] args)
        {
            Console.Title = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);

            Console.WriteLine("Initializing OpenGL on main thread...");
            _renderServer = new RenderServer();

            var serverThread = new Thread(() => MainAsync().GetAwaiter().GetResult())
            {
                Name = "Server Thread",
                IsBackground = true,
                Priority = ThreadPriority.Normal
            };
            serverThread.Start();
            
            _renderServer.RunRenderLoop();
        }

        static async Task MainAsync()
        {
            int clientId = 0;

            while (true)
            {
                try
                {
                    Console.WriteLine("Waiting for client to make a connection...");

                    var stream = new NamedPipeServerStream("StreamJsonRpcSamplePipe", PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await stream.WaitForConnectionAsync();

                    Console.WriteLine($"Client #{++clientId} connected. Starting handler...");
                    _ = Task.Run(() => ResponseToRpcRequestsAsync(stream, clientId));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in main loop: {ex.Message}");
                }
            }
        }

        private static async Task ResponseToRpcRequestsAsync(NamedPipeServerStream stream, int clientId)
        {
            try
            {
                await using (stream)
                {
                    var jsonRpc = JsonRpc.Attach(stream, _renderServer);
                    Console.WriteLine($"JSON-RPC attached to client #{clientId}");

                    jsonRpc.Disconnected += (sender, args) =>
                    {
                        Console.WriteLine($"Client #{clientId} disconnected: {args.Reason}");
                    };

                    await jsonRpc.Completion;
                    Console.WriteLine($"JSON-RPC completion for client #{clientId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client #{clientId}: {ex.Message}");
            }
        }
    }
}