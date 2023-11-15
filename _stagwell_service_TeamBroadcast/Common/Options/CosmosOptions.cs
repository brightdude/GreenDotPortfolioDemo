namespace Breezy.Muticaster
{
    public class CosmosOptions
    {
        public string ConnectionString { get; set; }

        public string ApplicationRegion { get; set; } = "West US 2";

        public string DatabaseName { get; set; } = "breezy-DB";
    }
}