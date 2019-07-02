using Functions.Model;
using System.Collections.Generic;

namespace Functions.Activities
{
    public class LogAnalyticsUploadActivityRequest
    {
        public IEnumerable<PreventiveLogItem> PreventiveLogItems { get; set; }
    }
}
