namespace MusicRoadmap.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<MusicRoadmapWebApplicationFactory>
{
    public const string Name = "IntegrationTests";
}
