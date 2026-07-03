namespace LitXus.IntegrationTests;

/// <summary>One shared ApiWebApplicationFactory (and its one throwaway database) for every test in this assembly.</summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<ApiWebApplicationFactory>;
