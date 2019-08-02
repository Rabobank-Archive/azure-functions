using System;
using AzDoCompliancy.CustomStatus;
using AzDoCompliancy.CustomStatus.Converter;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace Functions.Completeness.Activities
{
    public class GetTotalProjectCountFromSupervisorOrchestrationStatusActivity
    {
        [FunctionName(nameof(GetTotalProjectCountFromSupervisorOrchestrationStatusActivity))]
        public int? Run([ActivityTrigger] DurableOrchestrationStatus instanceToGetCountFrom)
        {
            if (instanceToGetCountFrom == null)
                throw new ArgumentNullException(nameof(instanceToGetCountFrom));

            var serializer = new JsonSerializer();
            serializer.Converters.Add(new CustomStatusConverter());
            
            return (instanceToGetCountFrom.CustomStatus?.ToObject<CustomStatusBase>(serializer) as
                SupervisorOrchestrationStatus)?.TotalProjectCount;
        }
    }
}


