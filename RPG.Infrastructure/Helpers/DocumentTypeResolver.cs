using Microsoft.Extensions.DependencyInjection;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Helpers;

public interface IDocumentTypeResolver
{
    (Type documentType, object mapper) GetMapping<TEntity>();
}

public class DocumentTypeResolver : IDocumentTypeResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, (Type documentType, Type mapperType)> _mappings = new();

    public DocumentTypeResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        foreach (var definition in DocumentMappingRegistry.All)
        {
            _mappings[definition.EntityType] = (definition.DocumentType, definition.MapperServiceType);
        }
    }

    public (Type documentType, object mapper) GetMapping<TEntity>()
    {
        if (!_mappings.TryGetValue(typeof(TEntity), out var mapping))
        {
            throw new InvalidOperationException($"No mapping registered for entity type {typeof(TEntity).Name}");
        }

        var mapper = _serviceProvider.GetRequiredService(mapping.mapperType);
        return (mapping.documentType, mapper);
    }
}
