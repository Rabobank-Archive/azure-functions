using System.Collections.Generic;
using Functions.Model;

namespace Functions.Activities
{
    public class LogAnalyticsUploadActivityRequest
    {
        public IEnumerable<PreventiveLogItem> PreventiveLogItems { get; set; }
    }
}
