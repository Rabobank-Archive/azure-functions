using ExpectedObjects;
using Functions.Model;
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
                                    Url =  "https://azuredevops.somewhere.azure.com"
                                },
                                Name = "NobodyCanDoAnything",
                                Status =  true
                            }
                        }
                    }
                }
            };

            var expected = new PreventiveLogItem
            {
                Project = "TAS",
                Scope = RuleScopes.Repositories,
                Item = "SOx-Compliant-Demo",
                Rule = "NobodyCanDoAnything",
                Status = true,
                EvaluatedDate = now,
                ScanId = scanId
            }.ToExpectedObject();

            var result = data.Flatten(RuleScopes.Repositories, scanId).Single();
            expected.ShouldEqual(result);
        }
    }
}