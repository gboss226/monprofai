namespace MonProfAI.Domain.Entities;

/// <summary>
/// Closed list of subjects a learner can flag as difficult during onboarding.
/// Kept closed on purpose: the platform targets minors, so we never store free text here (GDPR data minimisation).
/// </summary>
public static class SchoolSubjects
{
    public static readonly IReadOnlyList<string> All = new[]
    {
        "Mathématiques",
        "Français",
        "Histoire-Géographie",
        "Anglais",
        "SVT",
        "Physique-Chimie",
        "Technologie",
        "Autre",
    };
}
