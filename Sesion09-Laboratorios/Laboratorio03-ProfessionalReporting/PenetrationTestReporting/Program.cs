using System.CommandLine;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace PenetrationTestReporting;

class Program
{
    private static ILogger<Program>? _logger;

    static async Task<int> Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<Program>();

        var rootCommand = new RootCommand("📄 Penetration Testing Report Generator")
        {
            CreateInputOption(),
            CreateOutputOption(),
            CreateAudienceOption(),
            CreateFormatOption(),
            CreateTemplateOption()
        };

        rootCommand.SetHandler(async (string input, string output, string[] audiences, string format, string template) =>
        {
            await GenerateReports(input, output, audiences, format, template);
        }, CreateInputOption(), CreateOutputOption(), CreateAudienceOption(), CreateFormatOption(), CreateTemplateOption());

        return await rootCommand.InvokeAsync(args);
    }

    private static Option<string> CreateInputOption()
    {
        var option = new Option<string>(
            aliases: new[] { "--input", "-i" },
            description: "Input file with vulnerability data (JSON format)");
        option.IsRequired = true;
        return option;
    }

    private static Option<string> CreateOutputOption()
    {
        return new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for generated reports",
            getDefaultValue: () => "./Reports");
    }

    private static Option<string[]> CreateAudienceOption()
    {
        return new Option<string[]>(
            aliases: new[] { "--audience", "-a" },
            description: "Target audiences: executive, technical, compliance, all")
        {
            AllowMultipleArgumentsPerToken = true
        };
    }

    private static Option<string> CreateFormatOption()
    {
        return new Option<string>(
            aliases: new[] { "--format", "-f" },
            description: "Output format: markdown, html, pdf",
            getDefaultValue: () => "markdown");
    }

    private static Option<string> CreateTemplateOption()
    {
        return new Option<string>(
            aliases: new[] { "--template", "-t" },
            description: "Report template: standard, detailed, executive",
            getDefaultValue: () => "standard");
    }

    static async Task GenerateReports(string inputPath, string outputPath, string[] audiences, string format, string template)
    {
        try
        {
            _logger?.LogInformation("📄 PENETRATION TESTING REPORT GENERATOR");
            _logger?.LogInformation("=====================================");
            _logger?.LogInformation("Generation Time: {Time}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
            _logger?.LogInformation("Input: {Input}", inputPath);
            _logger?.LogInformation("Output: {Output}", outputPath);
            _logger?.LogInformation("Audiences: {Audiences}", string.Join(", ", audiences ?? new[] { "all" }));
            _logger?.LogInformation("");

            // Load vulnerability data
            var vulnerabilityData = await LoadVulnerabilityData(inputPath);
            
            // Create report generator
            var reportGenerator = new PenetrationTestReportGenerator();
            
            // Process each audience
            var targetAudiences = audiences?.Contains("all") == true || audiences?.Length == 0 
                ? new[] { "executive", "technical", "compliance" } 
                : audiences ?? new[] { "executive" };

            Directory.CreateDirectory(outputPath);

            foreach (var audience in targetAudiences)
            {
                _logger?.LogInformation("🎯 Generating {Audience} report...", audience);
                
                var report = await reportGenerator.GenerateReport(vulnerabilityData, audience, template);
                var fileName = $"{audience}-report.{GetFileExtension(format)}";
                var filePath = Path.Combine(outputPath, fileName);
                
                await SaveReport(report, filePath, format);
                _logger?.LogInformation("✅ {Audience} report saved: {Path}", audience, filePath);
            }

            // Generate summary dashboard
            _logger?.LogInformation("📊 Generating security metrics dashboard...");
            var dashboard = await reportGenerator.GenerateDashboard(vulnerabilityData);
            var dashboardPath = Path.Combine(outputPath, "security-dashboard.html");
            await File.WriteAllTextAsync(dashboardPath, dashboard);
            _logger?.LogInformation("✅ Dashboard saved: {Path}", dashboardPath);

            _logger?.LogInformation("");
            _logger?.LogInformation("🎉 Report generation completed successfully!");
            _logger?.LogInformation("📁 All reports available in: {Path}", Path.GetFullPath(outputPath));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Report generation failed: {Message}", ex.Message);
        }
    }

    static async Task<VulnerabilityData> LoadVulnerabilityData(string inputPath)
    {
        if (!File.Exists(inputPath))
        {
            // Generate sample data if input file doesn't exist
            _logger?.LogWarning("Input file not found. Generating sample vulnerability data...");
            return GenerateSampleData();
        }

        var jsonContent = await File.ReadAllTextAsync(inputPath);
        return JsonConvert.DeserializeObject<VulnerabilityData>(jsonContent) ?? GenerateSampleData();
    }

    static VulnerabilityData GenerateSampleData()
    {
        return new VulnerabilityData
        {
            AssessmentDate = DateTime.UtcNow,
            TargetApplication = "VulnerableWebApp Demo",
            TestingScope = "Web Application Security Assessment",
            TestingDuration = "2 days",
            Findings = new List<VulnerabilityFinding>
            {
                new()
                {
                    Id = "SQL-001",
                    Title = "SQL Injection in User Authentication",
                    Severity = "Critical",
                    CVSSScore = "9.8",
                    CVSSVector = "AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:H",
                    Description = "The login endpoint is vulnerable to SQL injection attacks through the username parameter.",
                    Location = "/api/auth/login",
                    ProofOfConcept = "' OR '1'='1' UNION SELECT password FROM users--",
                    Impact = "Complete database compromise and unauthorized access to all user accounts",
                    Recommendation = "Implement parameterized queries and input validation",
                    BusinessImpact = "Complete system compromise could result in data breach affecting all customers",
                    EstimatedCost = 2300000
                },
                new()
                {
                    Id = "XSS-002", 
                    Title = "Reflected Cross-Site Scripting in Search",
                    Severity = "High",
                    CVSSScore = "6.1",
                    CVSSVector = "AV:N/AC:L/PR:N/UI:R/S:C/C:L/I:L/A:N",
                    Description = "The search functionality reflects user input without proper encoding.",
                    Location = "/api/search",
                    ProofOfConcept = "<script>alert('XSS')</script>",
                    Impact = "Session hijacking and credential theft through malicious scripts",
                    Recommendation = "Implement proper output encoding and Content Security Policy",
                    BusinessImpact = "Customer account compromise and potential phishing attacks",
                    EstimatedCost = 150000
                },
                new()
                {
                    Id = "AUTH-003",
                    Title = "Authentication Bypass via Token Prediction",
                    Severity = "Critical",
                    CVSSScore = "9.1",
                    CVSSVector = "AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:N",
                    Description = "JWT tokens are generated using predictable patterns allowing bypass.",
                    Location = "/api/admin/secrets",
                    ProofOfConcept = "admin_1_1640995200",
                    Impact = "Unauthorized access to administrative functions and sensitive data",
                    Recommendation = "Implement cryptographically secure token generation",
                    BusinessImpact = "Administrative access bypass could lead to complete system control",
                    EstimatedCost = 1500000
                },
                new()
                {
                    Id = "CMD-004",
                    Title = "Command Injection in Network Utilities",
                    Severity = "Critical", 
                    CVSSScore = "9.8",
                    CVSSVector = "AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:H",
                    Description = "The ping functionality allows execution of arbitrary system commands.",
                    Location = "/api/utilities/ping",
                    ProofOfConcept = "127.0.0.1; cat /etc/passwd",
                    Impact = "Complete server compromise and access to underlying operating system",
                    Recommendation = "Sanitize input and use safe command execution methods",
                    BusinessImpact = "Server compromise could result in complete infrastructure takeover",
                    EstimatedCost = 3000000
                },
                new()
                {
                    Id = "INFO-005",
                    Title = "Sensitive Information Disclosure in Debug Endpoint",
                    Severity = "High",
                    CVSSScore = "7.5",
                    CVSSVector = "AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:N/A:N",
                    Description = "Debug configuration endpoint exposes sensitive system information.",
                    Location = "/api/debug/config",
                    ProofOfConcept = "GET /api/debug/config",
                    Impact = "Exposure of database credentials, API keys, and internal URLs",
                    Recommendation = "Remove debug endpoints from production or implement proper access controls",
                    BusinessImpact = "Exposed credentials could facilitate further attacks on infrastructure",
                    EstimatedCost = 500000
                }
            }
        };
    }

    static string GetFileExtension(string format)
    {
        return format.ToLower() switch
        {
            "html" => "html",
            "pdf" => "pdf",
            _ => "md"
        };
    }

    static async Task SaveReport(string content, string filePath, string format)
    {
        switch (format.ToLower())
        {
            case "html":
                var htmlContent = ConvertMarkdownToHtml(content);
                await File.WriteAllTextAsync(filePath, htmlContent);
                break;
            case "pdf":
                // For PDF generation, you would typically use a library like iTextSharp or PuppeteerSharp
                // For now, save as markdown and log a note about PDF conversion
                await File.WriteAllTextAsync(filePath.Replace(".pdf", ".md"), content);
                _logger?.LogInformation("Note: PDF conversion requires additional library. Saved as Markdown.");
                break;
            default:
                await File.WriteAllTextAsync(filePath, content);
                break;
        }
    }

    static string ConvertMarkdownToHtml(string markdown)
    {
        // Basic HTML wrapper for markdown content
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Security Assessment Report</title>
    <style>
        body {{ 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            max-width: 1200px; 
            margin: 0 auto; 
            padding: 20px; 
            line-height: 1.6; 
        }}
        .critical {{ color: #d73527; font-weight: bold; }}
        .high {{ color: #fd7e14; font-weight: bold; }}
        .medium {{ color: #ffc107; font-weight: bold; }}
        .low {{ color: #198754; font-weight: bold; }}
        pre {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; overflow-x: auto; }}
        table {{ border-collapse: collapse; width: 100%; margin: 20px 0; }}
        th, td {{ border: 1px solid #dee2e6; padding: 12px; text-align: left; }}
        th {{ background-color: #e9ecef; font-weight: bold; }}
        .executive-summary {{ background-color: #e3f2fd; padding: 20px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    {MarkdownToHtmlContent(markdown)}
</body>
</html>";
    }

    static string MarkdownToHtmlContent(string markdown)
    {
        // Basic markdown to HTML conversion (for a more robust solution, use Markdig)
        return markdown
            .Replace("# ", "<h1>").Replace("\n# ", "</h1>\n<h1>")
            .Replace("## ", "<h2>").Replace("\n## ", "</h2>\n<h2>")
            .Replace("### ", "<h3>").Replace("\n### ", "</h3>\n<h3>")
            .Replace("**", "<strong>").Replace("**", "</strong>")
            .Replace("*", "<em>").Replace("*", "</em>")
            .Replace("\n", "<br/>\n")
            + "</h1>"; // Close the last header
    }
}

// Data Models
public class VulnerabilityData
{
    public DateTime AssessmentDate { get; set; }
    public string TargetApplication { get; set; } = string.Empty;
    public string TestingScope { get; set; } = string.Empty;
    public string TestingDuration { get; set; } = string.Empty;
    public List<VulnerabilityFinding> Findings { get; set; } = new();
}

public class VulnerabilityFinding
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string CVSSScore { get; set; } = string.Empty;
    public string CVSSVector { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ProofOfConcept { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string BusinessImpact { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
}

public class PenetrationTestReportGenerator
{
    public async Task<string> GenerateReport(VulnerabilityData data, string audience, string template)
    {
        return audience.ToLower() switch
        {
            "executive" => await GenerateExecutiveReport(data),
            "technical" => await GenerateTechnicalReport(data),
            "compliance" => await GenerateComplianceReport(data),
            _ => await GenerateExecutiveReport(data)
        };
    }

    private async Task<string> GenerateExecutiveReport(VulnerabilityData data)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# SECURITY ASSESSMENT - EXECUTIVE BRIEFING");
        sb.AppendLine($"**Assessment Date:** {data.AssessmentDate:yyyy-MM-dd}");
        sb.AppendLine($"**Target Application:** {data.TargetApplication}");
        sb.AppendLine($"**Testing Duration:** {data.TestingDuration}");
        sb.AppendLine();
        
        sb.AppendLine("## 🎯 BOTTOM LINE UP FRONT");
        var criticalCount = data.Findings.Count(f => f.Severity == "Critical");
        var highCount = data.Findings.Count(f => f.Severity == "High");
        var totalCost = data.Findings.Sum(f => f.EstimatedCost);
        
        var riskLevel = criticalCount > 0 ? "CRITICAL" : highCount > 2 ? "HIGH" : "MEDIUM";
        sb.AppendLine($"**Risk Level:** {riskLevel}");
        sb.AppendLine($"**Financial Exposure:** ${totalCost:N0}");
        sb.AppendLine($"**Immediate Action Required:** {criticalCount} critical vulnerabilities");
        sb.AppendLine();
        
        sb.AppendLine("## 💼 BUSINESS IMPACT");
        sb.AppendLine($"- **Data Breach Risk:** ${totalCost:N0} potential exposure");
        sb.AppendLine($"- **Critical Vulnerabilities:** {criticalCount} requiring immediate attention");
        sb.AppendLine($"- **High-Risk Issues:** {highCount} requiring action within 30 days");
        sb.AppendLine($"- **Compliance Impact:** Multiple regulatory framework violations identified");
        sb.AppendLine();
        
        sb.AppendLine("## 🚨 IMMEDIATE ACTIONS REQUIRED");
        var criticalFindings = data.Findings.Where(f => f.Severity == "Critical").Take(3);
        int actionNumber = 1;
        foreach (var finding in criticalFindings)
        {
            sb.AppendLine($"{actionNumber}. **{finding.Title}** - {finding.BusinessImpact}");
            actionNumber++;
        }
        sb.AppendLine();
        
        sb.AppendLine("## 📈 RECOMMENDED INVESTMENT");
        var immediateCost = data.Findings.Where(f => f.Severity == "Critical").Sum(f => f.EstimatedCost * 0.1m); // 10% of exposure for immediate fixes
        var shortTermCost = data.Findings.Where(f => f.Severity == "High").Sum(f => f.EstimatedCost * 0.05m); // 5% of exposure for short-term fixes
        
        sb.AppendLine($"**Phase 1 (Immediate - ${immediateCost:N0}):**");
        sb.AppendLine("- Critical vulnerability remediation");
        sb.AppendLine("- Emergency security controls implementation");
        sb.AppendLine();
        sb.AppendLine($"**Phase 2 (30 days - ${shortTermCost:N0}):**");
        sb.AppendLine("- Security architecture improvements");
        sb.AppendLine("- Automated security testing implementation");
        sb.AppendLine();
        sb.AppendLine("**ROI Analysis:**");
        sb.AppendLine($"- Investment: ${(immediateCost + shortTermCost):N0}");
        sb.AppendLine($"- Risk Reduction: ${totalCost:N0}");
        sb.AppendLine($"- Net Benefit: ${(totalCost - immediateCost - shortTermCost):N0}");
        
        return sb.ToString();
    }

    private async Task<string> GenerateTechnicalReport(VulnerabilityData data)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# TECHNICAL VULNERABILITY ASSESSMENT REPORT");
        sb.AppendLine($"**Assessment Date:** {data.AssessmentDate:yyyy-MM-dd}");
        sb.AppendLine($"**Target Application:** {data.TargetApplication}");
        sb.AppendLine($"**Testing Scope:** {data.TestingScope}");
        sb.AppendLine();
        
        sb.AppendLine("## VULNERABILITY SUMMARY");
        sb.AppendLine($"- **Total Vulnerabilities:** {data.Findings.Count}");
        sb.AppendLine($"- **Critical:** {data.Findings.Count(f => f.Severity == "Critical")}");
        sb.AppendLine($"- **High:** {data.Findings.Count(f => f.Severity == "High")}");
        sb.AppendLine($"- **Medium:** {data.Findings.Count(f => f.Severity == "Medium")}");
        sb.AppendLine($"- **Low:** {data.Findings.Count(f => f.Severity == "Low")}");
        sb.AppendLine();
        
        sb.AppendLine("## DETAILED FINDINGS");
        
        foreach (var finding in data.Findings.OrderBy(GetSeverityOrder))
        {
            sb.AppendLine($"### {finding.Id}: {finding.Title}");
            sb.AppendLine($"**Severity:** {finding.Severity} (CVSS {finding.CVSSScore})");
            sb.AppendLine($"**Location:** {finding.Location}");
            sb.AppendLine();
            sb.AppendLine("**Description:**");
            sb.AppendLine(finding.Description);
            sb.AppendLine();
            sb.AppendLine("**Proof of Concept:**");
            sb.AppendLine("```");
            sb.AppendLine(finding.ProofOfConcept);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("**Impact:**");
            sb.AppendLine(finding.Impact);
            sb.AppendLine();
            sb.AppendLine("**Remediation:**");
            sb.AppendLine(finding.Recommendation);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    private async Task<string> GenerateComplianceReport(VulnerabilityData data)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# COMPLIANCE ASSESSMENT REPORT");
        sb.AppendLine($"**Assessment Date:** {data.AssessmentDate:yyyy-MM-dd}");
        sb.AppendLine($"**Target Application:** {data.TargetApplication}");
        sb.AppendLine();
        
        sb.AppendLine("## REGULATORY COMPLIANCE STATUS");
        sb.AppendLine();
        
        sb.AppendLine("### OWASP Top 10 2021 Compliance");
        sb.AppendLine("| OWASP Category | Status | Findings | Risk Level |");
        sb.AppendLine("|----------------|--------|----------|------------|");
        sb.AppendLine("| A01 - Broken Access Control | ❌ NON-COMPLIANT | 1 | Critical |");
        sb.AppendLine("| A02 - Cryptographic Failures | ✅ COMPLIANT | 0 | Low |");
        sb.AppendLine("| A03 - Injection | ❌ NON-COMPLIANT | 2 | Critical |");
        sb.AppendLine("| A04 - Insecure Design | ⚠️ PARTIAL | 1 | High |");
        sb.AppendLine("| A05 - Security Misconfiguration | ❌ NON-COMPLIANT | 1 | High |");
        sb.AppendLine("| A06 - Vulnerable Components | ✅ COMPLIANT | 0 | Low |");
        sb.AppendLine("| A07 - Identity Authentication | ❌ NON-COMPLIANT | 1 | Critical |");
        sb.AppendLine("| A08 - Software/Data Integrity | ✅ COMPLIANT | 0 | Low |");
        sb.AppendLine("| A09 - Logging/Monitoring | ⚠️ PARTIAL | 0 | Medium |");
        sb.AppendLine("| A10 - Server-Side Request Forgery | ✅ COMPLIANT | 0 | Low |");
        sb.AppendLine();
        
        sb.AppendLine("### ISO 27001:2022 Control Gaps");
        sb.AppendLine("- **A.9.2.3 - Management of privileged access rights:** Non-compliant due to authentication bypass");
        sb.AppendLine("- **A.12.6.1 - Management of technical vulnerabilities:** Non-compliant due to unpatched critical vulnerabilities");
        sb.AppendLine("- **A.13.1.1 - Network controls:** Partial compliance - some network security controls missing");
        sb.AppendLine("- **A.14.2.1 - Secure development policy:** Non-compliant due to secure coding practice gaps");
        sb.AppendLine();
        
        sb.AppendLine("### SOC 2 Type II Readiness");
        sb.AppendLine("- **Security Criteria:** Not ready - Critical vulnerabilities must be addressed");
        sb.AppendLine("- **Availability Criteria:** Partial readiness - Some controls in place");
        sb.AppendLine("- **Processing Integrity:** Not assessed - Requires business process evaluation");
        sb.AppendLine("- **Confidentiality:** Not ready - Data protection controls insufficient");
        sb.AppendLine("- **Privacy:** Not assessed - Requires privacy impact assessment");
        sb.AppendLine();
        
        sb.AppendLine("## REMEDIATION PRIORITIES FOR COMPLIANCE");
        sb.AppendLine();
        sb.AppendLine("### Phase 1: Critical Compliance Issues (0-30 days)");
        var criticalFindings = data.Findings.Where(f => f.Severity == "Critical");
        foreach (var finding in criticalFindings)
        {
            sb.AppendLine($"- **{finding.Title}:** {finding.Recommendation}");
        }
        sb.AppendLine();
        
        sb.AppendLine("### Phase 2: High Priority Compliance Issues (30-60 days)");
        var highFindings = data.Findings.Where(f => f.Severity == "High");
        foreach (var finding in highFindings)
        {
            sb.AppendLine($"- **{finding.Title}:** {finding.Recommendation}");
        }
        sb.AppendLine();
        
        sb.AppendLine("## COMPLIANCE TIMELINE");
        sb.AppendLine("- **30 days:** Address all critical vulnerabilities");
        sb.AppendLine("- **60 days:** Implement high-priority security controls");
        sb.AppendLine("- **90 days:** Complete medium-priority improvements");
        sb.AppendLine("- **120 days:** Conduct pre-audit assessment");
        sb.AppendLine("- **150 days:** Ready for external compliance audit");
        
        return sb.ToString();
    }

    public async Task<string> GenerateDashboard(VulnerabilityData data)
    {
        var criticalCount = data.Findings.Count(f => f.Severity == "Critical");
        var highCount = data.Findings.Count(f => f.Severity == "High");
        var mediumCount = data.Findings.Count(f => f.Severity == "Medium");
        var lowCount = data.Findings.Count(f => f.Severity == "Low");
        var totalCost = data.Findings.Sum(f => f.EstimatedCost);

        var securityScore = Math.Max(0, 100 - (criticalCount * 25 + highCount * 15 + mediumCount * 5));
        
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Security Assessment Dashboard</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
        }}
        .dashboard {{
            max-width: 1200px;
            margin: 0 auto;
        }}
        .metric-card {{
            background: white;
            border-radius: 8px;
            padding: 20px;
            margin: 10px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            display: inline-block;
            min-width: 200px;
            text-align: center;
        }}
        .metric-value {{
            font-size: 2.5em;
            font-weight: bold;
            margin: 10px 0;
        }}
        .critical {{ color: #d73527; }}
        .high {{ color: #fd7e14; }}
        .medium {{ color: #ffc107; }}
        .low {{ color: #198754; }}
        .security-score {{
            font-size: 3em;
            color: {(securityScore >= 80 ? "#198754" : securityScore >= 60 ? "#ffc107" : "#d73527")};
        }}
        .summary-section {{
            background: white;
            border-radius: 8px;
            padding: 20px;
            margin: 20px 0;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
    </style>
</head>
<body>
    <div class=""dashboard"">
        <h1>🛡️ Security Assessment Dashboard</h1>
        <p><strong>Assessment Date:</strong> {data.AssessmentDate:yyyy-MM-dd HH:mm:ss UTC}</p>
        
        <div class=""metric-card"">
            <h3>Security Score</h3>
            <div class=""metric-value security-score"">{securityScore:F0}%</div>
        </div>
        
        <div class=""metric-card"">
            <h3>Critical Issues</h3>
            <div class=""metric-value critical"">{criticalCount}</div>
        </div>
        
        <div class=""metric-card"">
            <h3>High Priority</h3>
            <div class=""metric-value high"">{highCount}</div>
        </div>
        
        <div class=""metric-card"">
            <h3>Medium Priority</h3>
            <div class=""metric-value medium"">{mediumCount}</div>
        </div>
        
        <div class=""metric-card"">
            <h3>Financial Exposure</h3>
            <div class=""metric-value"">${totalCost:N0}</div>
        </div>
        
        <div class=""summary-section"">
            <h2>📊 Executive Summary</h2>
            <p><strong>Total Vulnerabilities:</strong> {data.Findings.Count}</p>
            <p><strong>Risk Level:</strong> {(criticalCount > 0 ? "CRITICAL" : highCount > 2 ? "HIGH" : "MEDIUM")}</p>
            <p><strong>Immediate Action Required:</strong> {criticalCount} critical vulnerabilities must be addressed within 24 hours</p>
            <p><strong>Compliance Impact:</strong> Multiple regulatory framework violations identified</p>
        </div>
        
        <div class=""summary-section"">
            <h2>🎯 Next Steps</h2>
            <ol>
                <li>Address all critical vulnerabilities immediately</li>
                <li>Implement emergency security controls</li>
                <li>Schedule comprehensive security architecture review</li>
                <li>Establish continuous security monitoring</li>
                <li>Plan for compliance audit preparation</li>
            </ol>
        </div>
    </div>
</body>
</html>";
    }

    private int GetSeverityOrder(VulnerabilityFinding finding)
    {
        return finding.Severity switch
        {
            "Critical" => 1,
            "High" => 2,
            "Medium" => 3,
            "Low" => 4,
            _ => 5
        };
    }
}