using System.Collections.Generic;
using System.Linq;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Functions.Tests
{
    public class ProjectsTestHelper
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ProjectsTestHelper(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public static Project CreateProjectWithParameters(string name, string id, string description, string url)
        {
            var project = new Project
            {
                Name = name,
                Id = id,
                Description = description,
                Url = url
            };

            return project;
        }

        public static Project CreateDummyProject()
        {
            var project = new Project
            {
                Name = "Dummy project",
                Id = "1234",
                Description = "Describe project",
                Url = "www.url.com"
            };

            return project;
        }

        public static IEnumerable<Project> CreateMultipleProjectsResponse(int number)
        {
//            var items = new List<Project>();
//            for (int i = 0; i < number; i++)
//            {
//                items.Add(CreateDummyProject());
//            }

            return Enumerable
                 .Range(0, number)
                 .Select(_ => CreateDummyProject());  
        }

        [Fact]
        public void CreateMultipleProjectsResponseShouldCreateGivenNumberOfProjects()
        {
            var projects = CreateMultipleProjectsResponse(2);
            projects.Count().ShouldBe(2);

        }
    }
}