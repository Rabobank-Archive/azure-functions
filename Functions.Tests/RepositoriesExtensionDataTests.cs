using ExpectedObjects;
using Functions.Model;
using SecurePipelineScan.Rules.Security;
using System;
using System.Linq;
using Xunit;

namespace Functions.Tests
{
    public class RepositoriesExtensionDataTests
    {
        [Fact]
        public void FlattenReport()
        {
            var now = new DateTime(2019, 4, 29, 10, 47, 23);
            var scanId = "supId:projId:scope";
            var data = new ItemsExtensionData
            {
                Id = "TAS",
                Date = now,
                Reports = new[]
                {
                    new ItemExtensionData
                    {
                        Item = "SOx-Compliant-Demo",
                        Rules = new []
                        {
                            new EvaluatedRule
                            {
                                Description =  "Nobody can do anything",
                                Reconcile = new Reconcile
                                {
                                    Url =  new Uri("https://azuredevops.somewhere.azure.com")
                                },
                                Name = "NobodyCanDoAnything",
                                Status =  true
                            }
                        }
                    }
                }
            };

            var expected = new PreventiveRuleLogItem
            {
                EvaluatedDate = now,
                ScanDate = now,
                ScanId = "supId",
                Project = "TAS",
                ProjectId = "projId",
                Scope = RuleScopes.Repositories, 
                Item = "SOx-Compliant-Demo",
                Rule = "NobodyCanDoAnything",
                Status = true
            }.ToExpectedObject();

            var result = data.Flatten(RuleScopes.Repositories, scanId, "projId", now).Single();
            expected.ShouldEqual(result);
        }
    }
}