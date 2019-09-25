using Microsoft.WindowsAzure.Storage.Table;

namespace Functions.Model
{
    public class ConfigurationItem : TableEntity
    {
        public string CiIdentifier { get; set; }
        public string CiName { get; set; }
        public string BoDepartmentInfo { get; set; }
        public string BoNiveau1 { get; set; }
        public string BoNiveau2 { get; set; }
        public string BoNiveau3 { get; set; }
        public string BoNiveau4 { get; set; }
        public string BoNiveau5 { get; set; }
        public string SoDepartmentInfo { get; set; }
        public string SoNiveau1 { get; set; }
        public string SoNiveau2 { get; set; }
        public string SoNiveau3 { get; set; }
        public string SoNiveau4 { get; set; }
        public string SoNiveau5 { get; set; }

    }
}