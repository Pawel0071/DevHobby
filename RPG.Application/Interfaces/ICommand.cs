using RPG.Abstractions.Interfaces;

namespace RPG.Application.Interfaces;

public interface ICommand
{
}

public interface IMetadataCommand : ICommand
{
    CommandMetadata Metadata { get; set; }
}
