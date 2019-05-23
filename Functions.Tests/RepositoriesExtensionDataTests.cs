using System;
using System.Collections.Generic;
using System.Linq;
using ExpectedObjects;
using VstsLogAnalyticsFunction.Model;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests
{
    public class RepositoriesExtensionDataTests
    {
        [Fact]
        public void FlattenReport()
        {
            var now = new DateTime(2019, 4, 29, 10, 47, 23);
            var data = new ItemsExtensionData
            {
                Id = "TAS",
                Date = now,
                Reports = new []
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
                Scope = "repository",
                Item = "SOx-Compliant-Demo",
                Rule = "NobodyCanDoAnything",
                Status = true,
                EvaluatedDate = now
            }.ToExpectedObject();

            var result = data.Flatten("repository").Single();
            expected.ShouldEqual(result);
        }
    }
}