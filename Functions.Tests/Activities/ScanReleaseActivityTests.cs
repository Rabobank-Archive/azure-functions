using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Functions.Activities;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using System;
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
        public void ShouldReturnTrueForDifferentApproverAndCreator() 
        {
            //Arrange
            var release = _fixture.Create<Release>();

            //Act
            var fun = new ScanReleaseActivity();
            var result = fun.Run(release);

            //Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public void ShouldReturnFalseForSameApproverAndCreator()
        {
            //Arrange
            _fixture.Customize<Identity>(x => x
                .With(i => i.Id, Guid.NewGuid()));
            var release = _fixture.Create<Release>();

            //Act
            var fun = new ScanReleaseActivity();
            var result = fun.Run(release);

            //Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void ShouldReturnFalseWhenNoApprovedBy()
        {
            //Arrange
            _fixture.Customize<PreDeployApproval>(x => x
                .Without(a => a.ApprovedBy));
            var release = _fixture.Create<Release>();

            //Act
            var fun = new ScanReleaseActivity();
            var result = fun.Run(release);

            //Assert
            result.ShouldBeFalse();
        }
    }
}