using Functions.Model;

namespace Functions
{
    public class ExtensionDataUploadActivityRequest
    {
        public GlobalPermissionsExtensionData Data { get; set; }
        public string Scope { get; set; }
    }
}