using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using OccupancyService.Models;
using Microsoft.AspNet.SignalR;

namespace OccupancyService
{
    public class RoomsHub : Hub
    {
        private static List<string> _connections = new List<string>();

        public static IEnumerable<string> Connections => _connections;

        public override Task OnConnected()
        {
            _connections.Add(Context.ConnectionId);
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            _connections.Remove(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }
    }
}