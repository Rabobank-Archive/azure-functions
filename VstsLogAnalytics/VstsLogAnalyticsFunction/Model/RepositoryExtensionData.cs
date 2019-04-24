using System;

namespace VstsLogAnalyticsFunction.Model
{
    public class RepositoryExtensionData : ExtensionDataReports<EvaluatedRule>
    {
        public DateTime Date { get; internal set; }
        
        public string Token { get; set; }   
    }
}