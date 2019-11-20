using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Functions.Activities;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;

namespace Functions.Tests.Activities
{
    public class ScanReleaseActivityTests
    {
        private readonly Fixture _fixture;

        public ScanReleaseActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Fact]
        public void ShouldReturnFalseForReleaseWithoutApproval()
        {
            //Arrange
            _fixture.Customize<Approval>(x => x
                .Without(a => a.Approver));
            _fixture.Customize<ApprovalOptions>(x => x
                .With(a => a.ReleaseCreatorCanBeApprover, true));
            var release = _fixture.Create<Release>();           

            //Act
            var fun = new ScanReleaseActivity();
            var result = fun.Run(release);

            //Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void ShouldReturnFalseForAutomatedApproval()
        {
            //Arrange
            _fixture.Customize<Approval>(x => x
                .With(a => a.IsAutomated, true));
            _fixture.Customize<ApprovalOptions>(x => x
                .With(a => a.ReleaseCreatorCanBeApprover, false));
            var release = _fixture.Create<Release>();

            //Act
            var fun = new ScanReleaseActivity();
            var result = fun.Run(release);

            //Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void ShouldReturnFalseForWrongApprovalOptions()
        {
            //Arrange
            _fixture.Customize<Approval>(x => x
                .With(a => a.IsAutomated, false));
            _fixture.Customize<ApprovalOptions>(x => x
                .With(a => a.ReleaseCreatorCanBeApprover, true));
            var release = _fixture.Create<Release>();

            //Act
            var fun = new ScanReleaseActivity();
            var result = fun.Run(release);

            //Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void ShouldReturnTrueForOneCorrectApprovalAndApprovalOptions()
        {
            //Arrange
            _fixture.Customize<Approval>(x => x
                .With(a => a.IsAutomated, false));
            _fixture.Customize<ApprovalOptions>(x => x
                .With(a => a.ReleaseCreatorCanBeApprover, false));
            var release = _fixture.Create<Release>();

            //Act
            var fun = new ScanReleaseActivity();
            var result = fun.Run(release);

            //Assert
            result.ShouldBeTrue();
        }
    }
}