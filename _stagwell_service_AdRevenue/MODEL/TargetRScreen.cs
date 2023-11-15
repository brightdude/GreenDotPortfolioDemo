using System;
using System.Collections.Generic;
using System.Text;

namespace AdRevenueAggregation
{
    public class TargetRScreen
    {
        public string type { get; set; }
        public string id { get; set; }
        public string sequenceId { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public string gpsLongitude { get; set; }
        public string rtvTerminal { get; set; }
        public string rtvTeamViewer { get; set; }
        public string lastPlayerCommsMillis { get; set; }
        public string   requiredDataAvailable { get; set; }
        public string desiredPlayer { get; set; }
        public string wifiSsid { get; set; }
        public string locality { get; set; }
        public string timeZone { get; set; }
        public string lastLoaderCommsMillis { get; set; }
        public string rtvVenue_Location { get; set; }
        public string rtvCountry { get; set; }
        public string rtvAirportCode { get; set; }
        public string rtvVenueId { get; set; }
        public string rtvDemoAirportCode { get; set; }
        public string startCount { get; set; }
        public string memFree { get; set; }
        public string rtvGate_Area { get; set; }
        public string rtvScreenType { get; set; }
        public string hardwareDevice { get; set; }
        public string rtvWapStatus { get; set; }
        public string blobDataQueueSize { get; set; }
        public string rtvLBar { get; set; }
        public string   requiredDataTotal { get; set; }
        public string rtvAffiliate { get; set; }
        public string lastLoaderUdpCommsMillis { get; set; }
        public string modifiedMillis { get; set; }
        public string hardwareBuild { get; set; }
        public string rtvDedicated { get; set; }
        public string sequenceId { get; set; }
        public string startMillis { get; set; }
        public string hardwareModel { get; set; }
        public string rtvPuckType { get; set; }
        public string region { get; set; }
        public string rtvWapMac { get; set; }
        public string rtvState { get; set; }
        public string rtvConcessionaire { get; set; }
        public string rtvDemoConcessionaire { get; set; }
        public string customStatus { get; set; }
        public string street { get; set; }
        public string freeSpace_0 { get; set; }
        public string activePlayer { get; set; }
        public string freeSpace_1 { get; set; }
        public string gpsLatitude { get; set; }
        public string memTotal { get; set; }
        public string rtvCity { get; set; }
        public string rtvWapName { get; set; }
        public string label { get; set; }
        public string rtvScreen { get; set; }
        public string rtvWap { get; set; }
        public string blobDataDownloadFailures { get; set; }
        public string rtvAirportName { get; set; }
        public string rtvWapClients { get; set; }
        public string rtvServiceStatus { get; set; }
        public string online { get; set; }
        public string offline24 { get; set; }
        public string offline48 { get; set; }
        public string  commDiff { get; set; }
        public string  udpCommDiff { get; set; }
        public string   commStatus { get; set; }
        public string hdmiState { get; set; }
        public string localAddress { get; set; }
        public string subnetId { get; set; }
        public string   rtvComplianceCount { get; set; }
        public string loaderVersion { get; set; }
        public string backupServerUsed { get; set; }
        public string notes { get; set; }

        public string   healthStatus { get; set; }
        public string   firewallBlock { get; set; }
        public string   recentStartCount { get; set; }
        public string   applicationCrashed { get; set; }
        public string   blobDataDownloadedRate { get; set; }
        public string  loaderPlayerCommDiff { get; set; }

        public string displayDownloadedRate { get; set; }

        public string blobDataActiveDownloads { get; set; }
        public string   slowDownloadSpeed { get; set; }
        public string   localPeerCount { get; set; }
        public string   insufficientPeers { get; set; }
        public string   udpStatus { get; set; }
        public string   restartStatus { get; set; }

        public string   coportServiceStatus { get; set; }

        public string   rtvTeamViewerOn { get; set; }

        public string rtvHDMI { get; set; }

        public string rtvGate { get; set; }

        public string   numTVs { get; set; }
    }
}
