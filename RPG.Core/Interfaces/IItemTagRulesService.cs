namespace RPG.Core.Interfaces;

public interface IItemTagRulesService
{
    IEnumerable<Type> GetRequiredComponents(IList<string> tags);
}