using System;

namespace SecureBank.AuthAPI.Attributes;

/// <summary>
/// Atributo para habilitar rate limiting en endpoints (implementación básica)
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class EnableRateLimitingAttribute : Attribute
{
    public string PolicyName { get; }

    public EnableRateLimitingAttribute(string policyName)
    {
        PolicyName = policyName;
    }
} 