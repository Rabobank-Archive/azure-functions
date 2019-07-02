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
                Scope = "globalpermissions",
                Item = null,
                Rule = "NobodyCanDoAnything",
                Status = true,
                EvaluatedDate = now
            }.ToExpectedObject();

            var result = data.Flatten().Single();
            expected.ShouldEqual(result);
        }
    }
}