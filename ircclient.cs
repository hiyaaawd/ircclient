using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading; // Required for CancellationTokenSource
using System.Threading.Tasks;

namespace BidirectionalChat
{
    // ========================================================================
    //  Chat Client Class (C# 7.3 Compatible)
    // ========================================================================
    public class ChatClient
    {
        private TcpClient client; // Removed ?
        private NetworkStream stream; // Removed ?
        private string username = "DefaultUser";
        private CancellationTokenSource receiveCts; // Removed ?

        public async Task Connect(string ipAddress, int port, string username)
        {
            // (Code inside Connect remains largely the same, just be mindful of null checks)
             if (string.IsNullOrWhiteSpace(username))
            {
                Console.WriteLine("Username cannot be empty.");
                return;
            }
            this.username = username;
            client = new TcpClient();
            receiveCts = new CancellationTokenSource();

            try
            {
                Console.WriteLine($"Attempting to connect to {ipAddress}:{port} as {username}...");
                await client.ConnectAsync(ipAddress, port);
                stream = client.GetStream();
                Console.WriteLine($"Connected successfully!");

                byte[] usernameBytes = Encoding.UTF8.GetBytes(this.username);
                // Ensure stream is not null before writing
                if (stream != null) {
                    await stream.WriteAsync(usernameBytes, 0, usernameBytes.Length);
                } else {
                     Console.WriteLine("Error: Network stream is not available after connection.");
                     throw new InvalidOperationException("Network stream is null after successful connection.");
                }


                var receiveTask = ReceiveMessages(receiveCts.Token);
                await SendMessages();

                if (receiveCts != null) {
                    receiveCts.Cancel();
                    await Task.WhenAny(receiveTask, Task.Delay(1000));
                }

            }
            catch (SocketException ex)
            {
                Console.WriteLine($"\nError connecting to server: {ex.Message}");
                Console.WriteLine($"Please ensure the server is running at {ipAddress}:{port} and is reachable.");
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"\nAn unexpected error occurred during connection: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

         private async Task ReceiveMessages(CancellationToken cancellationToken)
        {
            // (Code inside ReceiveMessages remains largely the same)
            // ... Ensure null checks for client and stream ...
            byte[] buffer = new byte[4096];
            int bytesRead;

            Console.WriteLine("Listening for messages...");
            try
            {
                while (client != null && client.Connected && stream != null && stream.CanRead && !cancellationToken.IsCancellationRequested)
                // ... rest of the method ...
                {
                     try
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        // ... rest of try ...
                         if (bytesRead > 0)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            ClearCurrentConsoleLine();
                            Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] {message}");
                            Console.Write("> ");
                        }
                        else
                        {
                            Console.WriteLine("\nConnection closed by the server.");
                            break;
                        }
                    }
                    // ... catch blocks ...
                     catch (OperationCanceledException) { /* ... */ break; }
                     catch (System.IO.IOException ioEx) { /* ... */ break; }
                     catch (ObjectDisposedException) { /* ... */ break; }
                }
            }
             // ... catch blocks ...
            catch (Exception ex) when (!(ex is OperationCanceledException || ex is System.IO.IOException || ex is ObjectDisposedException)) { /* ... */ }
            finally { /* ... */ }
        }

        private async Task SendMessages()
        {
            // (Code inside SendMessages remains largely the same)
            // ... Ensure null checks for client and stream ...
            string messageToSend; // Removed ?

            Console.WriteLine("You can now type messages. Type 'exit' to quit.");
            Console.Write("> ");

             try
            {
                while (client != null && client.Connected && stream != null && stream.CanWrite)
                {
                     messageToSend = await Task.Run(() => Console.ReadLine());

                    if (messageToSend != null && messageToSend.ToLowerInvariant() == "exit")
                    {
                        Console.WriteLine("Exiting chat...");
                        break;
                    }
                     // Add null check for messageToSend before IsNullOrWhiteSpace
                     if (messageToSend != null && !string.IsNullOrWhiteSpace(messageToSend) && stream != null && stream.CanWrite)
                     {
                          // ... rest of send logic ...
                           byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);
                           // ... try/catch WriteAsync ...
                            try
                            {
                                await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                                Console.Write("> ");
                            }
                            catch (System.IO.IOException ioEx) { /* ... */ break; }
                            catch (ObjectDisposedException) { /* ... */ break; }
                     }
                     // ... rest of loop ...
                      else if (client != null && client.Connected && stream != null && stream.CanWrite)
                     {
                         Console.Write("> ");
                     }
                }
            }
            // ... catch and finally ...
             catch(Exception ex) { /* ... */ }
            finally { /* ... */ }
        }

        private void Disconnect()
        {
            // (Code inside Disconnect remains largely the same)
            // ... Ensure null checks for client, stream, receiveCts ...
             Console.WriteLine("Disconnecting...");
            try
            {
                 if(receiveCts != null)
                 {
                    receiveCts.Cancel();
                    receiveCts.Dispose();
                 }

                 if(stream != null)
                 {
                    stream.Close();
                    stream.Dispose();
                 }

                 if (client != null && client.Connected)
                 {
                    client.Close();
                 }
                 if(client != null)
                 {
                    client.Dispose();
                 }
            }
            catch(Exception ex) { /* ... */ }
            finally
            {
                stream = null;
                client = null;
                Console.WriteLine("Disconnected.");
            }
        }

        // Helper to clear the current line (no changes needed)
        private static void ClearCurrentConsoleLine() { /* ... */ }
    }

    // ========================================================================
    //  Chat Server Class (C# 7.3 Compatible)
    // ========================================================================
    public class ChatServer
    {
        private TcpListener listener; // Removed ?
        private readonly Dictionary<TcpClient, string> clients = new Dictionary<TcpClient, string>();
        private readonly object clientsLock = new object();
        private readonly object consoleLock = new object();
        private CancellationTokenSource serverShutdownCts; // Removed ?

        public async Task Start(int port)
        {
            // (Code inside Start remains largely the same)
             serverShutdownCts = new CancellationTokenSource();
             listener = new TcpListener(IPAddress.Any, port);
             // ... try/catch listener.Start() ...
              try
            {
                 listener.Start();
                // ... console output ...
                 lock (consoleLock) { /* ... */ }
                 // ... Task.Run HandleServerCommands ...
                  _ = Task.Run(() => HandleServerCommands(serverShutdownCts.Token));
                 // ... while loop accepting clients ...
                  while (serverShutdownCts != null && !serverShutdownCts.IsCancellationRequested)
                  {
                      // ... try/catch AcceptTcpClientAsync ...
                       try
                        {
                            TcpClient client = await listener.AcceptTcpClientAsync();
                             // ... console output ...
                              lock (consoleLock) { /* ... */ }
                             // ... HandleClient ...
                              _ = HandleClient(client, serverShutdownCts.Token);
                        }
                        // ... catch blocks ...
                         catch (InvalidOperationException ex) when (serverShutdownCts != null && serverShutdownCts.IsCancellationRequested) { /* ... */ break; }
                         catch (SocketException ex) { /* ... */ await Task.Delay(100); }
                  }
            }
             // ... catch blocks ...
              catch (SocketException ex) { /* ... */ }
             catch (Exception ex) { /* ... */ }
             finally
             {
                // ... listener?.Stop() ...
                 if (listener != null) listener.Stop();
                 // ... console output ...
                  lock (consoleLock) { /* ... */ }
                 // ... DisconnectAllClients ...
                  await DisconnectAllClients("Server shutting down (final cleanup).");
                 // ... console output ...
                   lock (consoleLock) { /* ... */ }
             }
        }

         private async Task HandleClient(TcpClient client, CancellationToken serverToken)
        {
            // (Code inside HandleClient remains largely the same)
             string username = "Unknown";
            NetworkStream stream = null; // Removed ? - initialize to null
            CancellationTokenSource linkedCts = null; // Initialize to null

            try
            {
                 // Add null check for serverToken before creating linked token source
                 if (serverToken == null) throw new ArgumentNullException(nameof(serverToken));
                 linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serverToken);

                 stream = client.GetStream();
                 // ... Set timeouts ...
                  stream.ReadTimeout = 15000;

                 // ... Read username ...
                  byte[] usernameBuffer = new byte[256];
                 int bytesRead;
                 // ... try/catch ReadAsync ...
                  try { bytesRead = await stream.ReadAsync(usernameBuffer, 0, usernameBuffer.Length, linkedCts.Token); }
                 catch (OperationCanceledException) when (serverToken.IsCancellationRequested) { /* ... */ return; }
                 catch (Exception ex) when (ex is System.IO.IOException || ex is OperationCanceledException) { /* ... */ client.Close(); return; }

                 // ... Reset timeouts ...
                  stream.ReadTimeout = Timeout.Infinite;
                 // ... Process username ...
                  if (bytesRead > 0) { /* ... logic to set username, check duplicates, add to clients, broadcast ... */ }
                 else { /* ... handle empty username data ... */ client.Close(); return; }

                 // ... Read messages loop ...
                  byte[] buffer = new byte[4096];
                  // Add null check for linkedCts
                  while (client.Connected && stream != null && stream.CanRead && linkedCts != null && !linkedCts.Token.IsCancellationRequested)
                  {
                       // ... try/catch ReadAsync for messages ...
                        try { bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, linkedCts.Token); /* ... process message, broadcast ... */}
                       catch (OperationCanceledException) when (linkedCts.Token.IsCancellationRequested) { /* ... */ break; }
                       catch (System.IO.IOException ioEx) { /* ... */ break; }
                       catch (ObjectDisposedException) { /* ... */ break; }
                  }
            }
            // ... catch block ...
            catch (Exception ex) when (!(ex is ObjectDisposedException || ex is OperationCanceledException)) { /* ... */ }
            finally
            {
                // ... Cleanup logic (remove client, broadcast left, close stream/client) ...
                 // Ensure null check for linkedCts before disposing
                 if(linkedCts != null) try { linkedCts.Dispose(); } catch { /* Ignore */ }
            }
        }

        // Helper to send a message to a single client stream (no changes needed)
         private async Task SendPrivateMessage(NetworkStream stream, string message, CancellationToken token) { /* ... */ }

        // Broadcast (sender parameter should be nullable even in C# 7.3 for logic)
         private async Task Broadcast(string message, TcpClient sender) // Removed ? from TcpClient here
        {
            // (Code inside Broadcast remains largely the same)
             byte[] messageBytes = Encoding.UTF8.GetBytes(message);
             List<TcpClient> clientsSnapshot = new List<TcpClient>();
             // --- Safely get snapshot ---
             lock (clientsLock) { clientsSnapshot.AddRange(clients.Keys); }
             // --- End Lock ---
             List<Task> broadcastTasks = new List<Task>();
             foreach (var client in clientsSnapshot)
             {
                  // Add null check for sender != null before comparing
                  bool isSender = (sender != null && client == sender);
                  if (!isSender && client.Connected)
                  {
                       broadcastTasks.Add(Task.Run(async () => {
                            try {
                                 // ... GetStream, WriteAsync ...
                            }
                             // ... catch blocks ...
                             catch (Exception ex) when (ex is System.IO.IOException || ex is ObjectDisposedException) { /* ... */ }
                             catch(Exception ex) { /* ... */ }
                       }));
                  }
             }
             await Task.WhenAll(broadcastTasks);
        }

       // HandleServerCommands (no changes needed for nullability here)
        private async Task HandleServerCommands(CancellationToken shutdownToken) { /* ... */ }

        // ProcessServerCommand (args parameter should be string, check for null/empty)
         private async Task ProcessServerCommand(string command, CancellationToken shutdownToken)
         {
             string[] parts = command.Trim().Split(new[] { ' ' }, 2);
             string mainCommand = parts[0].ToLowerInvariant();
             string args = parts.Length > 1 ? parts[1] : null; // Removed ? - assign null if no args

             if (shutdownToken.IsCancellationRequested) return;

             switch (mainCommand)
             {
                 // ... cases ...
                 case "/say":
                     // Use IsNullOrWhiteSpace(args) check
                     if (!string.IsNullOrWhiteSpace(args)) { /* ... */ }
                     else { /* ... */ }
                     break;
                 case "/kick":
                      // Use IsNullOrWhiteSpace(args) check
                      if (!string.IsNullOrWhiteSpace(args)) { await KickUser(args, "Kicked by server administrator."); }
                      else { /* ... */ }
                      break;
                  // ... other cases ...
             }
         }

        // ListUsers (no changes needed)
         private void ListUsers() { /* ... */ }

        // KickUser (no changes needed for nullability here)
         private async Task KickUser(string usernameToKick, string reason) { /* ... */ }

        // DisconnectAllClients (no changes needed)
         private async Task DisconnectAllClients(string reason) { /* ... */ }

        // ShutdownServer (no changes needed for nullability here)
         private async Task ShutdownServer() { /* ... */ }
    }

    // ========================================================================
    //  Program Entry Point (C# 7.3 Compatible)
    // ========================================================================
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Bidirectional Chat";
            Console.WriteLine("--- Bidirectional TCP Chat ---");
            Console.WriteLine("Choose mode: (S)erver or (C)lient");
            string mode = Console.ReadLine()?.Trim().ToUpperInvariant(); // Keep ?. here as ReadLine can return null

            try
            {
                if (mode == "S") { /* ... server setup ... */ }
                else if (mode == "C")
                {
                     Console.Write("Enter the server IP address (e.g., 127.0.0.1 for localhost): ");
                     string ipAddress = Console.ReadLine()?.Trim(); // Keep ?.
                     // Check ipAddress for null/whitespace after ReadLine
                      if (string.IsNullOrWhiteSpace(ipAddress)) { /* ... */ return; }

                     Console.Write("Enter the server port (e.g., 8888): ");
                     if (int.TryParse(Console.ReadLine(), out int port) && port > 0 && port < 65536)
                     {
                         Console.Write("Enter your username: ");
                         string username = Console.ReadLine()?.Trim(); // Keep ?.
                          // Check username for null/whitespace after ReadLine
                          if (string.IsNullOrWhiteSpace(username)) { /* ... */ return; }

                         Console.Title = $"Chat Client ({username}@{ipAddress}:{port})";
                         ChatClient client = new ChatClient();
                         await client.Connect(ipAddress, port, username);
                     }
                     else { /* ... */ }
                }
                else { /* ... */ }
            }
            catch (Exception ex) { /* ... */ }
            finally { /* ... */ }
        }
    }
}