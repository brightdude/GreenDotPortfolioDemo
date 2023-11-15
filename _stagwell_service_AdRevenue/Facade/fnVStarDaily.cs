using System;
using System.Data;
using System.Data.Common;
using System.IO;
using Ardalis.GuardClauses;
using System.IO.Compression;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Dapper;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using AdRevenueAggregation;
using AdRevenueAggregation.MODEL;
using AdRevenueAggregation.Managers;



namespace AdRevenueAggregation
{
    public static class fnVStarDaily
    {
        public static List<string> AllFilesEverProceesed { get; set; } = new List<string>();
        public static ILogger Log;
        private static string CONNECTIONSTRING = AppHelper.GetSecretFromAzureKeyVault("VSTARCONNECTIONSTRING", "https://your-keyvault-name.vault.azure.net");

        public static string CONTAINERNAME = AppHelper.GetSecretFromAzureKeyVault("VSTARCONTAINERNAME", "https://your-keyvault-name.vault.azure.net");

        [FunctionName("fnVStarDaily")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            Guard.Against.Null(Log, nameof(Log));
            log.LogInformation("C# HTTP trigger function processed a request fnVStarDaily");
            Task.Factory.StartNew(() => { Process(); });
            return new OkObjectResult("Started catchup process for the daily VStarDaily and catchup");
        }
        public static void Process()
        {

            try
            {
                AllFilesEverProceesed = DBRepo.VStar_getFilename();
                Log.LogInformation("INIT Client");
                GetCSVs();
            }
            catch (Exception e)
            {
                Log.LogInformation($"!!!!!!!!!! EXCEPTION is thrown {e.Message} !!!!!!!!!!");
                Log.LogError(1150, e, e.Message);
            }
        }
        private static void GetCSVs()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CONNECTIONSTRING);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(CONTAINERNAME);

            var _containerFiles = container.ListBlobs().ToList().Select(s => s.StorageUri.PrimaryUri.LocalPath).ToList();
            _containerFiles.ForEach(file => {
                if ( !AllFilesEverProceesed.Contains(file))
                {
                    ProcessFile(file, container);
                }
            });
            DBRepo.VStarCommitCatchup_Async().Wait();

        }
        private static void ProcessFile(string filename, CloudBlobContainer container)
        {
            string _filename = filename.Replace(@"/vistarlogs/", "");
            CloudBlockBlob blockBlobReference = container.GetBlockBlobReference(_filename);
            string csvRaw;

            using (var memoryStream = new MemoryStream())
            {
                blockBlobReference.DownloadToStream(memoryStream);
                csvRaw = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                SavetoDBFeedCSV(csvRaw);
                DBRepo.VStar_SetFilename(filename).Wait();
            }
        }
        private static void SavetoDBFeedCSV(string fileText)
        {
            try
            {

                List<string> lines=fileText.Split('\n').ToList().Skip(6).ToList();// first 6 line is the header
                lines = lines.Take(lines.Count - 2).ToList();/// Last line is junk that doesn't split into 15 elements


                lines.ForEach(line => {
                    var row = line.Split(',');
                    VStarRecord record = new VStarRecord();
                    record.Day = row[0].ToString();
                    record.BidderName = row[1].ToString();
                    record.Buyer = row[2].ToString();
                    record.Advertiser = row[3].ToString();
                    record.Creative = row[4].ToString();
                    record.CreativeID = row[5].ToString();
                    record.VenueID = row[6].ToString();
                    record.VenueName = row[7].ToString();
                    record.Impressions = row[8].ToString();
                    record.Spots = row[9].ToString();
                    record.Revenue = row[10].ToString();
                    record.eCPM = row[11].ToString();
                    record.DataeCPM = row[12].ToString();
                    record.DataCost = row[13].ToString();
                    record.Profit = row[14].ToString();

                    DBRepo.VStarSet_Async(record).Wait();

                });
            }
            catch (Exception e)
            {
                Log.LogInformation($"!!!!!!!!!! EXCEPTION is thrown {e.Message} !!!!!!!!!!");
            }
        }

    }
}
