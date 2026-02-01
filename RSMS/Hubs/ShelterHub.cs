using Microsoft.AspNetCore.SignalR;

namespace RSMS.Hubs
{
    public class ShelterHub:Hub
    {
        public async Task BroadCastData(object payload) 
        {
            await Clients.All.SendAsync("ShelterUpdated", payload);
        }
    }
}
