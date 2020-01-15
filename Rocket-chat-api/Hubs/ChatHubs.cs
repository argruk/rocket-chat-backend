﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Rocket_chat_api.Hubs
{
    public class ChatHub : Hub
        {
            public async Task SendMessage(string user, string message)
            {
                await Clients.All.SendAsync("ReceiveMessage", user, message);
            }
        }
}