namespace RPG.Domain.Models.Skills.SkillComponents;

/// <summary>
///     Component for skill resource costs and requirements.
///     Pure data - resource validation handled by services.
/// </summary>
public class ResourceCostComponent : ISkillComponent
{
    public Dictionary<string, int> Costs { get; set; } = new(); // resource type -> amount
    public bool ConsumeOnCast { get; set; } = true;
    public bool RefundOnInterrupt { get; set; } = true;
    public float RefundPercentage { get; set; } = 100f;
    public Dictionary<string, int> GeneratesResources { get; set; } = new(); // For skills that generate resources
}
