using System;
using System.Data;
using System.Data.Common;
using System.IO;
using Ardalis.GuardClauses;

using System.IO.Compression;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using System.Linq;

using System.Text;
using System.Text.Encodings;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


using Amazon.S3;
using Amazon.S3.Model;
using ExcelDataReader;
using CsvHelper;
using CsvHelper.Configuration;

using AdRevenueAggregation;
using AdRevenueAggregation.MODEL;
using AdRevenueAggregation.Managers;


namespace AdRevenueAggregation
{
    public static class fnPlaceExchangeDaily
    {
        //string SpringServePassword = 
        private static string AMAZONACCESSKEY = AppHelper.GetSecretFromAzureKeyVault("AMAZONACCESSKEY", "https://your-keyvault-name.vault.azure.net");
        private static string AMAZONSECRETKEY = AppHelper.GetSecretFromAzureKeyVault("AMAZONSECRETKEY", "https://your-keyvault-name.vault.azure.net");
        private static string AMAZONBUCKETNAME = AppHelper.GetSecretFromAzureKeyVault("AMAZONBUCKETNAME", "https://your-keyvault-name.vault.azure.net");
        private static string PREFIX = "out/";
        private static ILogger Log;
        private static Microsoft.Azure.WebJobs.ExecutionContext ctx;
        private static List<string> allFilesEverProceesed { get; set; } = new List<string>();

        [FunctionName("fnPlaceExchange")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,ILogger log)
        {
            Guard.Against.Null(Log, nameof(Log));
            log.LogInformation("C# HTTP trigger function processed a request. fnPlaceExchange");

            Guard.Against.Null(req, nameof(req));
            Task.Factory.StartNew(() => { Process(); });
            log.LogInformation("Started catchup process for the daily PlaceExchange and catchup");
            return new OkObjectResult("Started catchup process for the daily PlaceExchange and catchup");

        }
        public static void Process()
        {
            try
            {
                Guard.Against.Null(Log, nameof(Log));
                allFilesEverProceesed = DBRepo.PlaceExchange_getFilename();
                Guard.Against.Null(allFilesEverProceesed, nameof(allFilesEverProceesed));
                ListObjectsRequest requestToListObjects = new ListObjectsRequest()
                {
                    BucketName = AMAZONBUCKETNAME,
                    Prefix = PREFIX,
                    MaxKeys = 1000,

                };
                var contToken = new System.Threading.CancellationToken();
                Log.LogInformation("INIT Client");
                AmazonS3Client amazonClient = new AmazonS3Client(AMAZONACCESSKEY, AMAZONSECRETKEY, Amazon.RegionEndpoint.USEast1);

                do
                {
                    Log.LogInformation(">>>: FETCHING next 1000 objects from S3 Bucket... =^+^= ");

                    var listObjectsResponse = amazonClient.ListObjectsAsync(requestToListObjects, contToken).Result;
                    var objectsFetchedSoFar = listObjectsResponse.S3Objects;
                    Guard.Against.Null(objectsFetchedSoFar, nameof(objectsFetchedSoFar));

                    foreach (var s3Object in objectsFetchedSoFar)
                    {
                        ProcessS3File(amazonClient, s3Object, contToken);
                    }
                    if (listObjectsResponse.IsTruncated)
                    {
                        requestToListObjects.Marker = listObjectsResponse.NextMarker;
                    }
                    else
                    {
                        requestToListObjects = null;
                    }
                } while (requestToListObjects != null);

                DBRepo.PlaceExchangeCommitCatchup_Async().Wait();
                Log.LogInformation(">>>:  ALL DONE: All available files are processed, going into sleep-mode ====");
            }
            catch (Exception e)
            {
                Log.LogInformation($"!!!!!!!!!! EXCEPTION is thrown {e.Message} !!!!!!!!!!");
                Log.LogError(1150, e, e.Message);
                throw e;
            }

        }

       private static void ProcessS3File(AmazonS3Client amazonClient, S3Object s3Object, CancellationToken contToken)
        {

            Guard.Against.Null(amazonClient, nameof(amazonClient));
            Guard.Against.Null(s3Object, nameof(s3Object));
            Guard.Against.Null(contToken, nameof(contToken));

            string filePath = string.Empty, fileDir = string.Empty, unGzfilePath = string.Empty;
            string objectKey = s3Object.Key;
            string objectBucketName = s3Object.BucketName;
            Log.LogInformation($">>>>>>> PROCESSING KEY: {objectKey}");

            try
            {


                if ((objectKey.EndsWith(".xlsx") || objectKey.EndsWith(".csv")) && !allFilesEverProceesed.Contains(objectKey))
                {
                    string extn, content;
                    readFileByExtention(amazonClient, s3Object, objectKey, out extn, out content);
                    if (extn == "xlsx")
                    {
                        SavetoDBFeedExcel(filePath);
                    }
                    else
                    {
                        SavetoDBFeedCSV(objectKey, content);
                    }
                    DBRepo.PlaceExchange_SetFilename(objectKey).Wait();
                }
                else
                {
                    Log.LogInformation($"<<<<<<< DIDN'T PROCESESS THE FILE: {s3Object.Key}   =+=+=>>>");
                }
            }
            catch (Exception e)
            {
                var message = e.Message;
                Log.LogInformation($">>>>>>>>>>  DIDN'T PROCESESS THE FILE because of error: {s3Object.Key}   =+=+=>>>");
            }
        }

        private static void readFileByExtention(AmazonS3Client amazonClient, S3Object s3Object, string objectKey, out string extn, out string content)
        {
            Guard.Against.Null(amazonClient, nameof(amazonClient));
            Guard.Against.Null(s3Object, nameof(s3Object));
            Guard.Against.NullOrWhiteSpace(objectKey, nameof(objectKey));

            extn = (objectKey.EndsWith(".xlsx")) ? "xlsx" : "csv";
            GetObjectRequest getRequest = new GetObjectRequest() { BucketName = s3Object.BucketName, Key = s3Object.Key };
            GetObjectResponse listResponse = amazonClient.GetObjectAsync(getRequest).Result;

            Guard.Against.Null(listResponse, "listResponse");
            Guard.Against.Null(listResponse.ResponseStream, "listResponse.ResponseStream");

            StreamReader reader = new StreamReader(listResponse.ResponseStream);
            content = reader.ReadToEnd();
        }

        private static void SavetoDBFeedExcel(string filePath)
        {
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    using (var reader = ExcelReaderFactory.CreateOpenXmlReader(stream))
                    {
                        var Rows = reader.AsDataSet().Tables[0].Rows;
                        foreach (DataRow row in Rows)
                        {
                            SaveRecord(row);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogInformation($"!!!!!!!!!! EXCEPTION is thrown {e.Message} !!!!!!!!!!");
                throw e;
            }
        }

        private static void SavetoDBFeedCSV(string filename, string content)
        {
            try
            {
                string header = "";
                using (StringReader reader = new StringReader(content))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {

                        if ( String.IsNullOrEmpty(header))
                        {
                            // get header
                            header = line;
                        } else
                        {
                            PlaceExchangeRecord record = new PlaceExchangeRecord();
                            line = strRemoveCommaFromLongInt(line);
                            var row = line.Split(',');
                            if (!header.Contains("Creative ID"))
                            {
                                record = NoCreativeID(filename, row);
                            }
                            else
                            {
                                record = WithCreativeID(filename, row);
                            }
                            if (!String.IsNullOrEmpty(record.OrgName) && record.OrgName == "ReachTV") /// skip first row CheapTrick
                            {
                                DBRepo.PlaceExchangeSetAsync(record).Wait();
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Log.LogInformation($"!!!!!!!!!! EXCEPTION is thrown {e.Message} !!!!!!!!!!");
            }
        }
  

        private static PlaceExchangeRecord NoCreativeID(string filename, string[] row)
        {
            PlaceExchangeRecord record = new PlaceExchangeRecord();
            var _playTsDate = filename.Substring(33, 10).Split('-');

            record.OrgName = row[1].ToString();
            record.Advertiser = row[2].ToString();
            record.AdunitName = row[3].ToString();
            record.PlayTsDate = new DateTime(Int32.Parse(_playTsDate[0]), Int32.Parse(_playTsDate[1]), Int32.Parse(_playTsDate[2])).ToShortDateString();
            record.PayerName = row[4].ToString();
            record.NetworkName = row[5].ToString();
            record.BuyerName = row[6].ToString();
            record.CreativeURL = row[7].ToString();
            record.CreativePreview = row[8].ToString();
            record.NumDistinctImpressions = row[9].ToString();
            record.NumDistinctPlays = row[10].ToString();
            record.ClearingPriceAmount = row[11].ToString();
            record.MediaCostAmount = row[12].ToString();
            record.PubClearingPriceEcpm = row[13].ToString();
            record.PubMediaCostEcpm = row[14].ToString();
            return record;
        }
        private static PlaceExchangeRecord WithCreativeID(string filename, string[] row)
        {
            var _playTsDate = filename.Substring(33, 10).Split('-');
            PlaceExchangeRecord record = new PlaceExchangeRecord();
            record.OrgName = row[1].ToString();
            record.Advertiser = row[2].ToString();
            record.AdunitName = row[3].ToString();
            record.PlayTsDate = new DateTime(Int32.Parse(_playTsDate[0]), Int32.Parse(_playTsDate[1]), Int32.Parse(_playTsDate[2])).ToShortDateString();
            record.PayerName = row[4].ToString();
            record.NetworkName = row[5].ToString();
            record.BuyerName = row[6].ToString();
            record.CreativeURL = row[7].ToString();
            record.CreativePreview = row[8].ToString();
            record.CreativeID = row[9].ToString();
            record.NumDistinctImpressions = row[10].ToString();
            record.NumDistinctPlays = row[11].ToString();
            record.ClearingPriceAmount = row[12].ToString();
            record.MediaCostAmount = row[13].ToString();
            record.PubClearingPriceEcpm = row[14].ToString();
            record.PubMediaCostEcpm = row[15].ToString();
            return record;
        }

        private static string strRemoveCommaFromLongInt(string line)
        {
            var reg = new Regex("\".*?\"");
            var matches = reg.Matches(line);
            foreach (var item in matches)
            {
                var with = item.ToString();
                var without = with.Replace(",", "").Replace("\"", "");
                line = line.Replace(with, without);
            }

            return line;
        }

        private static void SaveRecord(DataRow row)
        {
            PlaceExchangeRecord record = new PlaceExchangeRecord();
            record.OrgName = row[1].ToString();
            record.Advertiser = row[2].ToString();
            record.AdunitName = row[3].ToString();
            record.PlayTsDate = row[4].ToString();
            record.PayerName = row[5].ToString();
            record.NetworkName = row[6].ToString();
            record.BuyerName = row[7].ToString();
            ///record.CreativeID = row[8].ToString();
            record.CreativeURL = row[9].ToString();
            record.CreativePreview = row[10].ToString();
            record.NumDistinctImpressions = row[11].ToString();
            record.NumDistinctPlays = row[12].ToString();
            record.ClearingPriceAmount = row[13].ToString();
            record.MediaCostAmount = row[14].ToString();
            record.PubClearingPriceEcpm = row[15].ToString();
            record.PubMediaCostEcpm = row[16].ToString();
            if (record.OrgName == "ReachTV")
            {/// skip first row CheapTrick
                DBRepo.PlaceExchangeSetAsync(record).Wait();
            }
        }
    }
}
