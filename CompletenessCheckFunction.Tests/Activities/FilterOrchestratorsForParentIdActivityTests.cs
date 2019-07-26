using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace CompletenessCheckFunction.Tests.Activities
{
    public class FilterOrchestratorsForParentIdActivityTests
    {
        private readonly Fixture _fixture;
        public FilterOrchestratorsForParentIdActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }
    }
}