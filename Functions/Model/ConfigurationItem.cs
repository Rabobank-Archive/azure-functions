using Microsoft.WindowsAzure.Storage.Table;

namespace Functions.Model
{
    public class ConfigurationItem : TableEntity
    {
        public string CiIdentifier { get; set; }
        public string DepartmentInfo { get; set; }
        public string Niveau1 { get; set; }
        public string Niveau2 { get; set; }
        public string Niveau3 { get; set; }
        public string Niveau4 { get; set; }
        public string Niveau5 { get; set; }
    }
}