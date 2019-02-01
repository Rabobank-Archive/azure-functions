using Moq;
using SecurePipelineScan.VstsService;
using Shouldly;
using System;
using Xunit;

namespace VstsLogAnalytics.Common.Tests
{
    public class VstsRestRatedClientTests
    {
        [Fact]
        public void Get()
        {
            var clientMock = new Mock<IVstsRestClient>();
            var requestMock = new Mock<IVstsRestRequest<int>>();

            clientMock.Setup(x => x.Get(requestMock.Object)).Returns(6);

            var sut = new VstsRestRatedClient(clientMock.Object);

            var result = sut.Get(requestMock.Object);

            result.ShouldBe(6);
        }

        [Fact]
        public void Post()
        {
            var clientMock = new Mock<IVstsRestClient>();
            var requestMock = new Mock<IVstsPostRequest<int>>();

            clientMock.Setup(x => x.Post(requestMock.Object)).Returns(6);

            var sut = new VstsRestRatedClient(clientMock.Object);

            var result = sut.Post(requestMock.Object);

            result.ShouldBe(6);
        }

        [Fact]
        public void Get_ShouldThrottle()
        {
            var clientMock = new Mock<IVstsRestClient>();
            var requestMock = new Mock<IVstsRestRequest<int>>();

            clientMock.Setup(x => x.Get(requestMock.Object)).Returns(6);

            var sut = new VstsRestRatedClient(clientMock.Object, TimeSpan.FromSeconds(10), 2);

            for (int i = 0; i < 10; i++)
            {
                var result = sut.Get(requestMock.Object);
                result.ShouldBe(6);
            }
        }
    }
}