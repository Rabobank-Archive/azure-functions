using SecurePipelineScan.VstsService.Response;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Xunit;
using System;

namespace Functions.Tests
{
    public class ProjectsTestHelper
    {
        public static Project CreateProjectWithParameters(string name, string id, string description, Uri url)
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

        private static Project CreateDummyProject()
        {
            var fixture = new Fixture();
            return fixture.Create<Project>();
        }

        public static IEnumerable<Project> CreateMultipleProjectsResponse(int number)
        {
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