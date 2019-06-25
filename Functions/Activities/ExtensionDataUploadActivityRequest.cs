using Functions.Model;

namespace Functions
{
    internal class ExtensionDataUploadActivityRequest
    {
        public GlobalPermissionsExtensionData Data { get; set; }
        public string Scope { get; set; }
    }
}