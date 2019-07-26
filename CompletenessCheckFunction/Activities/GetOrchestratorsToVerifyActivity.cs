using System;
using System.Collections.Generic;
using System.Linq;
using DurableFunctionsAdministration.Client;
using DurableFunctionsAdministration.Client.Model;
using DurableFunctionsAdministration.Client.Request;
using DurableFunctionsAdministration.Client.Response;
using Microsoft.Azure.WebJobs;

namespace CompletenessCheckFunction.Activities
{
    public class GetOrchestratorsToVerifyActivity
    {
        private readonly IDurableFunctionsAdministrationClient _client;
        
        public GetOrchestratorsToVerifyActivity(IDurableFunctionsAdministrationClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(_client));
        }
        
        [FunctionName(nameof(GetOrchestratorsToVerifyActivity))]
        public List<OrchestrationInstance> Run([ActivityTrigger] DurableOrchestrationContextBase context)
        {
            var orchestrators = _client.Get(OrchestrationInstances.List())
                .Where(i => i.Name == "ProjectScanSupervisor" && i.RuntimeStatus == RunTimeStatusses.Completed)
                .ToList();
            
            return orchestrators;
        }
    }
}