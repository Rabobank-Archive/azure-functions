using ExpectedObjects;
using Functions.Model;
using System;
using System.Linq;
using Xunit;

namespace Functions.Tests
{
    public class GlobalPermissionsExtensionDataTests
    {
        [Fact]
        public void FlattenReport()
        {
            var now = new DateTime(2019, 4, 29, 10, 47, 23);
            var scanId = "supId:projId:scope";
            var data = new GlobalPermissionsExtensionData
            {
                Id = "TAS",
                Date = now,
                Reports = new[]
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
            };

            var expected = new PreventiveLogItem
            {
                Project = "TAS",
                Scope = RuleScopes.GlobalPermissions,
                Item = null,
                Rule = "NobodyCanDoAnything",
                Status = true,
                ScanId = scanId,
                EvaluatedDate = now
            }.ToExpectedObject();

            var result = data.Flatten(scanId).Single();
            expected.ShouldEqual(result);
        }
    }
}