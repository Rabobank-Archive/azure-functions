using System;
using System.Collections.Generic;

namespace VstsLogAnalyticsFunction.Model
{
    public class RepositoriesExtensionData : ExtensionDataReports<RepositoryExtensionData>
    {
        public DateTime Date { get; set; }
    }

    public class RepositoryExtensionData
    {
        public string Item { get; set; }
        public IList<EvaluatedRule> Rules { get; set; }
    }
}