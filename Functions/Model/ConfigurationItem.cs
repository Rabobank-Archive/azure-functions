using Microsoft.WindowsAzure.Storage.Table;

namespace Functions.Model
{
    public class ConfigurationItem : TableEntity
    {
        public string CiIdentifier { get; set; }
        public string DepartmentInfo { get; set; }
        public string Level1 { get; set; }
        public string Level2 { get; set; }
        public string Level3 { get; set; }
        public string Level4 { get; set; }
        public string Level5 { get; set; }
    }
}