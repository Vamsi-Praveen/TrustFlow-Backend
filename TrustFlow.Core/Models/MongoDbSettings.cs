namespace TrustFlow.Core.Models
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public bool SeedData { get; set; } = false;
    }
}
