using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Socket.ChatProtocol;

int clientId = 1;

// Địa chỉ IP và cổng của server
// Tạo endpoint để lắng nghe kết nối từ client
var endPoint = new IPEndPoint(IPAddress.Loopback, Constants.DefaultChatPort);

var serverSocket = new System.Net.Sockets.Socket(
    endPoint.AddressFamily,
    SocketType.Stream,
    ProtocolType.Tcp
);

try
{
    serverSocket.Bind(endPoint);

    Console.WriteLine($"Listening... (port {endPoint.Port})");

    serverSocket.Listen();
    
    var clientHandlers = new List<Task>();

    while (true)
    {
        var clientSocket = await serverSocket.AcceptAsync();
        var t = HandleClientRequestAsync(clientSocket, clientId++);
        clientHandlers.Add(t);
    }

    Task.WaitAll([.. clientHandlers]);

}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}


static async Task HandleClientRequestAsync(System.Net.Sockets.Socket clientSocket, int clientId)
{
    Console.WriteLine($"[Client {clientId}] connected!");

    var welcomeBytes = Encoding.UTF8.GetBytes(Constants.WelcomeText);
    await clientSocket.SendAsync(welcomeBytes);
    
    var buffer = new byte[1024];
    
    while (true)
    {
        var bytesReceived = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);
        var msg = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

        if (msg.Equals(Constants.CommandShutdown, StringComparison.OrdinalIgnoreCase))
        {
            CloseConnection(clientSocket);
            Console.WriteLine($"[Client {clientId}] disconnected!");
            break;
        }

        Console.WriteLine($"[Client {clientId}]: {msg}");

        // Tạo phản hồi dựa trên yêu cầu của client
        var response = HandlerMessageRequest(msg);
        var responseJson = JsonSerializer.Serialize(response);
        var responseBytes = Encoding.UTF8.GetBytes(responseJson);
        
        LoggingResponse(response, clientId);
        
        Console.WriteLine($"[Client {clientId}] Sending response: {responseJson}");
        
        // Gửi phản hồi cho client
        await clientSocket.SendAsync(new ArraySegment<byte>(responseBytes), SocketFlags.None);
    }
}

static Response HandlerMessageRequest(string message)
{
    if (string.IsNullOrWhiteSpace(message))
    {
        return new Response
        {
            Message = "Empty message received.",
            Status = false,
            Error = "Invalid input.",
            Code = 400
        };
    }

    return new Response
    {
        Message = $"Server received: {message}",
        Status = true,
        Error = null,
        Code = 200
    };
}

static void LoggingResponse(Response response, int clientId)
{
    if (string.IsNullOrWhiteSpace(response.Message))
    {
        Console.WriteLine($"[Client {clientId}] Response message: {response.Message}");
    }
    
    Console.WriteLine($"[Client {clientId}] Response status: {response.Status}");
    
    if(string.IsNullOrWhiteSpace(response.Error))
    {
        Console.WriteLine($"[Client {clientId}] Response error: {response.Error}");
    }
    
    Console.WriteLine($"[Client {clientId}] Response code: {response.Code}");
}

static void CloseConnection(System.Net.Sockets.Socket clientSocket)
{
    try
    {
        clientSocket.Shutdown(SocketShutdown.Both);
    }
    catch (SocketException)
    {
        
    }
    finally
    {
        clientSocket.Close();
    }
}

class Response
{
    public string? Message { get; set; }
    public string? Error { get; set; }
    public bool Status { get; set; }
    public int Code { get; set; }
}