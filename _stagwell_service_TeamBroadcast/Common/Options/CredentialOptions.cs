using Newtonsoft.Json;

namespace Breezy.Muticaster
{
    public class CredentialOptions
    {
        public  CredentialOptions ()
        {
            ScheduledEventService = new BasicCredential();
            WaitingRoomService = new BasicCredential();
            OnDemandMeetingService = new BasicCredential();
        }

        public BasicCredential ScheduledEventService { get; set; }

        public BasicCredential WaitingRoomService { get; set; }

        public BasicCredential OnDemandMeetingService { get; set; }

        public string GraphServiceJson { get; set; }

        public ApplicationCredential GraphService
            => JsonConvert.DeserializeObject<ApplicationCredential>(GraphServiceJson);
    }   
}