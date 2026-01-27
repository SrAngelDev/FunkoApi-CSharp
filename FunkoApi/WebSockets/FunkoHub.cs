using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.SignalR;

namespace FunkoApi.WebSockets;

[ExcludeFromCodeCoverage]
public class FunkoHub : Hub
{
    // Cuando alguien se conecta
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        Console.WriteLine($"Cliente conectado: {Context.ConnectionId}");
    }
}