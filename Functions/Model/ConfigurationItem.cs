using Microsoft.Azure.Cosmos.Table;

namespace Functions.Model
{
    public class ConfigurationItem : TableEntity
    {
        public string CiIdentifier { get; set; }
        public string CiName { get; set; }
        public string BoDepartmentInfo { get; set; }
        public string BusinessOwnerName { get; set; }
        public string BoLevel1 { get; set; }
        public string BoLevel2 { get; set; }
        public string BoLevel3 { get; set; }
        public string BoLevel4 { get; set; }
        public string BoLevel5 { get; set; }
        public string SoDepartmentInfo { get; set; }
        public string SystemOwnerName { get; set; }
        public string SoLevel1 { get; set; }
        public string SoLevel2 { get; set; }
        public string SoLevel3 { get; set; }
        public string SoLevel4 { get; set; }
        public string SoLevel5 { get; set; }
    }
}