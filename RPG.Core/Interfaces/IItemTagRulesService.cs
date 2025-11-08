namespace RPG.Core.Interfaces;

public interface ITagRulesService
{
    IEnumerable<Type> GetRequiredComponents(IList<string> tags);
}
