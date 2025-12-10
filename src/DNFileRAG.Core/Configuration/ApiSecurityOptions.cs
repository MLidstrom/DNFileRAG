namespace DNFileRAG.Core.Configuration;

public class ApiSecurityOptions
{
    public const string SectionName = "ApiSecurity";

    public bool RequireApiKey { get; set; } = true;
    public List<ApiKeyConfig> ApiKeys { get; set; } = [];
}

public class ApiKeyConfig
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "reader";
}
