using NetworkTester.Models;

namespace NetworkTester.Services;

public interface ISecurityRuleAnalyzer
{
    Task AnalyzeSecurityRulesAsync(string vmName, bool showEffective = false);
    Task<SecurityAnalysisReport> GenerateSecurityAnalysisAsync(string resourceGroup);
    Task<List<SecurityViolation>> ValidateSecurityPostureAsync(string resourceGroup);
}

public class SecurityAnalysisReport
{
    public string ResourceGroup { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    public List<SecurityRuleAnalysis> RuleAnalysis { get; set; } = new();
    public List<SecurityViolation> Violations { get; set; } = new();
    public SecurityPostureScore PostureScore { get; set; } = new();
    public List<SecurityRecommendation> Recommendations { get; set; } = new();
}

public class SecurityRuleAnalysis
{
    public string ResourceName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public List<EffectiveSecurityRule> EffectiveRules { get; set; } = new();
    public List<SecurityIssue> Issues { get; set; } = new();
}

public class SecurityViolation
{
    public string ViolationType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Remediation { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
}

public class SecurityPostureScore
{
    public int OverallScore { get; set; } // 0-100
    public int NetworkSegmentationScore { get; set; }
    public int AccessControlScore { get; set; }
    public int DefaultRulesScore { get; set; }
    public string OverallRating => OverallScore switch
    {
        >= 90 => "Excellent",
        >= 80 => "Good",
        >= 70 => "Fair",
        >= 60 => "Poor",
        _ => "Critical"
    };
}

public class SecurityRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty; // High, Medium, Low
    public string Category { get; set; } = string.Empty;
    public string Implementation { get; set; } = string.Empty;
}

public class SecurityIssue
{
    public string IssueType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
} 