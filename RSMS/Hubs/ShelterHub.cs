using Microsoft.AspNetCore.SignalR;

namespace RSMS.Hubs
{
    public class ShelterHub:Hub
    {
        //Client joins a specific shelter group
        public async Task JoinShelterGroup(string shelterCode) 
        {
            if (string.IsNullOrWhiteSpace(shelterCode))
                return;
            await Groups.AddToGroupAsync(Context.ConnectionId, shelterCode);
        }

        //Optional: Client leaves a specific shelter group
        public async Task LeaveShelterGroup(string shelterCode) 
        {
            if(string.IsNullOrWhiteSpace(shelterCode))
                return;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, shelterCode);
        }
    }
}
