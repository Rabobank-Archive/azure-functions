// using AutoFixture;
// using Newtonsoft.Json;
// using NSubstitute;
// using Functions.Cmdb;
// using Functions.Cmdb.Client;
// using Functions.Cmdb.Model;
// using SecurePipelineScan.VstsService;
// using SecurePipelineScan.VstsService.Response;
// using Shouldly;
// using System.Collections.Generic;
// using System.Linq;
// using Xunit;
// using Response = SecurePipelineScan.VstsService.Response;
// using Task = System.Threading.Tasks.Task;
// using Moq;
// using SecurePipelineScan.VstsService.Security;

// namespace Functions.Cmdb.Tests
// {
//     public class ReconcileReleasePipelineHasDeploymentMethodFunctionTests
//     {

//         [Fact]
//         public async Task WhenValidUserThenCanUpdateDeploymentInfo()
//         {
//             // Arrange
//             const string NonProdCiIdentifier = "CI9999";
//             const string NonProdConfigurationItem = "AZDO-NON-PROD";

//             const string ProdCiIdentifier = "CI1234567";
//             const string ProdConfigurationItem = "TEST";

//             const string Organization = "somecompany";
//             const string ProjectId = "1";
//             const string PipelineId = "2";
//             const string UserId = "2dbe73bd-5f5c-6152-b980-1b9e87449188";
//             const string AssignmentGroup = "TAS";
//             const string AzDoDeploymentMethod = "Azure Devops";

//             var ci = new CiContentItem() { Device = new ConfigurationItemModel { ConfigurationItem = ProdConfigurationItem, AssignmentGroup = AssignmentGroup } };

//             var supplementaryInfo = new SupplementaryInformation { Organization = Organization, Project = ProjectId, Pipeline = PipelineId };
//             DeploymentInfo azdoDeploymentInfo = new DeploymentInfo
//             {
//                 DeploymentMethod = AzDoDeploymentMethod,
//                 SupplementaryInformation = JsonConvert.SerializeObject(supplementaryInfo)
//             };

//             var nonProdCi = new CiContentItem()
//             {
//                 Device = new ConfigurationItemModel
//                 {
//                     ConfigurationItem = NonProdConfigurationItem,
//                     AssignmentGroup = AssignmentGroup,
//                     DeploymentInfo = new[] { azdoDeploymentInfo }
//                 }
//             };

//             var cmdbClient = Substitute.For<ICmdbClient>();
//             cmdbClient.GetCiAsync(ProdCiIdentifier).Returns(Task.FromResult(ci));

//             cmdbClient.GetCiAsync(NonProdCiIdentifier).Returns(Task.FromResult(nonProdCi));
//             cmdbClient.Config.Returns(new CmdbClientConfig("abc", "https://localhost", Organization, NonProdCiIdentifier));

//             var assignment = new AssignmentContentItem { Assignment = new Assignment { Operators = new List<string> { "x.y@somecompany.nl" } } };
//             cmdbClient.GetAssignmentAsync(AssignmentGroup).Returns(Task.FromResult(assignment));

//             var vstsClient = Substitute.For<IVstsRestClient>();
//             vstsClient.GetAsync(Arg.Any<IVstsRequest<UserEntitlement>>()).Returns(Task.FromResult(new UserEntitlement { User = new User { PrincipalName = "x.y@somecompany.nl" } }));



//             ManageProjectPropertiesPermission(fixture);

//             var tokenizer = new Mock<ITokenizer>();
//             tokenizer
//                 .Setup(x => x.Principal(It.IsAny<string>()))
//                 .Returns(PrincipalWithClaims());

//             var vstsClient = new Mock<IVstsRestClient>();
//             vstsClient
//                 .Setup(x => x.GetAsync(It.Is<IVstsRequest<Response.PermissionsProjectId>>(req =>
//                     req.QueryParams.Values.Contains("TAS"))))
//                 .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()))
//                 .Verifiable();

//             var rule = new ReconcileReleasePipelineHasDeploymentMethodFunction(vstsClient, cmdbClient);

//             // Act
//             dynamic data = new { environment = "9", ciIdentifier = ProdCiIdentifier };
//             await rule.ReconcileAsync(ProjectId, PipelineId, "3", UserId, data);

//             // Assert
//             await cmdbClient.Received().GetCiAsync(ProdCiIdentifier);
//             await cmdbClient.Received().GetAssignmentAsync(AssignmentGroup);
//             await cmdbClient.Received().GetCiAsync(NonProdCiIdentifier);
//             await cmdbClient.Received().UpdateDeploymentMethodAsync(NonProdConfigurationItem,
//                 Arg.Is<CiContentItem>(c => c.Device.DeploymentInfo.Count() == 1 &&
//                     c.Device.DeploymentInfo.First().DeploymentMethod == null &&
//                     c.Device.DeploymentInfo.First().SupplementaryInformation == null)
//                 );
//             await cmdbClient.Received().UpdateDeploymentMethodAsync(ProdConfigurationItem, Arg.Is<CiContentItem>(c => c.Device.DeploymentInfo.Count() == 1 && c.Device.DeploymentInfo.First().DeploymentMethod == AzDoDeploymentMethod));
//         }

//         [Fact]
//         public async Task WhenAzDoNonProdPipelineThenSkipsAuthorization()
//         {
//             // Arrange
//             var ci = new CiContentItem() { Device = new ConfigurationItemModel { ConfigurationItem = "AZDO-NON-PROD-PIPELINES", AssignmentGroup = "TAS" } };
//             var cmdbClient = Substitute.For<ICmdbClient>();
//             cmdbClient.GetCiAsync("CI1234").Returns(Task.FromResult(ci));
//             cmdbClient.Config.Returns(new CmdbClientConfig("abc", "https://localhost", "somecompany", "CI1234"));

//             var assignment = new AssignmentContentItem { Assignment = new Assignment { Operators = new List<string> { "x.y@somecompany.nl" } } };
//             cmdbClient.GetAssignmentAsync("TAS").Returns(Task.FromResult(assignment));

//             var vstsClient = Substitute.For<IVstsRestClient>();
//             vstsClient.GetAsync(Arg.Any<IVstsRequest<UserEntitlement>>()).Returns(Task.FromResult(new UserEntitlement { User = new User { PrincipalName = "notautorised@somecompany.nl" } }));
//             var rule = new ReleasePipelineHasDeploymentMethod(vstsClient, cmdbClient);

//             // Act
//             await rule.ReconcileAsync("1", "2", "3", "2dbe73bd-5f5c-6152-b980-1b9e87449188");

//             // Assert
//             await cmdbClient.Received().GetCiAsync("CI1234");
//             await cmdbClient.Received().GetAssignmentAsync("TAS");
//             await cmdbClient.Received().UpdateDeploymentMethodAsync("AZDO-NON-PROD-PIPELINES",
//                 Arg.Is<CiContentItem>(c => c.Device.DeploymentInfo.Count() == 1 &&
//                 c.Device.DeploymentInfo.First().DeploymentMethod == "Azure Devops"));
//         }

//         [Fact]
//         public async Task WhenNonProdPipelineAlreadyRegistratedThenNoDoubleRegistration()
//         {
//             // Arrange
//             var supplementaryInfo = new SupplementaryInformation { Organization = "somecompany", Project = "1", Pipeline = "2" };
//             var azdoDeploymentInfo = new DeploymentInfo
//             {
//                 DeploymentMethod = "Azure Devops",
//                 SupplementaryInformation = JsonConvert.SerializeObject(supplementaryInfo)
//             };

//             var ci = new CiContentItem() { Device = new ConfigurationItemModel { ConfigurationItem = "AZDO-NON-PROD-PIPELINES", AssignmentGroup = "TAS", DeploymentInfo = new[] { azdoDeploymentInfo } } };

//             var cmdbClient = Substitute.For<ICmdbClient>();
//             cmdbClient.GetCiAsync("CI1234").Returns(Task.FromResult(ci));
//             cmdbClient.Config.Returns(new CmdbClientConfig("abc", "https://localhost", "somecompany", "CI1234"));

//             var assignment = new AssignmentContentItem { Assignment = new Assignment { Operators = new List<string> { "x.y@somecompany.nl" } } };
//             cmdbClient.GetAssignmentAsync("TAS").Returns(Task.FromResult(assignment));

//             var vstsClient = Substitute.For<IVstsRestClient>();
//             vstsClient.GetAsync(Arg.Any<IVstsRequest<UserEntitlement>>()).Returns(Task.FromResult(new UserEntitlement { User = new User { PrincipalName = "notautorised@somecompany.nl" } }));
//             var rule = new ReleasePipelineHasDeploymentMethod(vstsClient, cmdbClient);

//             // Act
//             await rule.ReconcileAsync("1", "2", "3", "2dbe73bd-5f5c-6152-b980-1b9e87449188");

//             // Assert
//             await cmdbClient.Received().GetCiAsync("CI1234");
//             await cmdbClient.Received().GetAssignmentAsync("TAS");
//             await cmdbClient.DidNotReceive().UpdateDeploymentMethodAsync("AZDO-NON-PROD-PIPELINES", Arg.Any<CiContentItem>());
//         }
//     }
// }