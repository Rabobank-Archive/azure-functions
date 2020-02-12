using System.Threading.Tasks;
using Xunit;
using Shouldly;
using Functions.Model;
using Functions.Activities;
using AutoFixture;
using Response = SecurePipelineScan.VstsService.Response;
using System.Linq;
using Moq;
using Functions.Cmdb.ProductionItems;

namespace Functions.Tests.Activities
{
    public class GetDeploymentMethodsActivityTests
    {
        [Fact]
        public async Task ShouldReturnNonProdForNonProdCI()
        {
            // Arrange 
            var fixture = new Fixture();
            var project = fixture.Create<Response.Project>();
            var organization = fixture.Create<string>();

            fixture.Customize<DeploymentMethod>(ctx => ctx
                .With(x => x.Organization, organization)
                .With(x => x.ProjectId, project.Id)
                .With(x => x.CiIdentifier, "CI12345"));

            var repo = new Mock<IProductionItemsRepository>();


            repo
    .SetupSequence(x => x.GetAsync(It.IsAny<string>()))
    .ReturnsAsync(fixture.CreateMany<DeploymentMethod>(3).ToList());

            // Act
            var fun = new GetDeploymentMethodsActivity(
                new EnvironmentConfig { Organization = organization, NonProdCiIdentifier = "CI12345" }, repo.Object);
            var result = await fun.RunAsync(project.Id);

            //Assert
            result.Count.ShouldBe(3);
            result.ShouldContain(x => x.DeploymentInfo.Any(d => d.CiIdentifier == "NON-PROD" && d.StageId == "NON-PROD"));
        }
    }
}