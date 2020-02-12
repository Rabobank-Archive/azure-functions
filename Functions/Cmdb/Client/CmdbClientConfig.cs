namespace Functions.Cmdb.Client
{
    public class CmdbClientConfig
    {
        public CmdbClientConfig(string apiKey, string endpoint, string organization, string nonProdCiIdentifier)
        {
            this.ApiKey = apiKey;
            this.Endpoint = endpoint;
            this.Organization = organization;
            this.NonProdCiIdentifier = nonProdCiIdentifier;
        }

        public string ApiKey { get; set; }
        public string Endpoint { get; set; }
        public string Organization { get; set; }
        public string NonProdCiIdentifier { get; set; }
    }
}