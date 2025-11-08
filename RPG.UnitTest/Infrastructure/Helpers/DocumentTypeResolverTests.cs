using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Helpers;
using RPG.Infrastructure.Interfaces;
using System;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Helpers;

public class DocumentTypeResolverTests
{
    private readonly IDocumentTypeResolver _resolver;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public DocumentTypeResolverTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IDocumentMapper<Character, CharacterDocument>)))
            .Returns(new Mock<IDocumentMapper<Character, CharacterDocument>>().Object);
        
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IDocumentMapper<Item, ItemDocument>)))
            .Returns(new Mock<IDocumentMapper<Item, ItemDocument>>().Object);

        serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        serviceScopeFactoryMock.Setup(s => s.CreateScope()).Returns(serviceScopeMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactoryMock.Object);
        
        _resolver = new DocumentTypeResolver(_serviceProviderMock.Object);
    }

    [Fact]
    public void GetMapping_ForCharacter_ReturnsCharacterDocumentAndMapper()
    {
        // Act
        var (documentType, mapper) = _resolver.GetMapping<Character>();

        // Assert
        documentType.Should().Be(typeof(CharacterDocument));
        mapper.Should().NotBeNull();
        mapper.Should().BeAssignableTo<IDocumentMapper<Character, CharacterDocument>>();
    }

    [Fact]
    public void GetMapping_ForItem_ReturnsItemDocumentAndMapper()
    {
        // Act
        var (documentType, mapper) = _resolver.GetMapping<Item>();

        // Assert
        documentType.Should().Be(typeof(ItemDocument));
        mapper.Should().NotBeNull();
        mapper.Should().BeAssignableTo<IDocumentMapper<Item, ItemDocument>>();
    }

    [Fact]
    public void GetMapping_ForNonExistentDocument_ThrowsException()
    {
        // Act & Assert
        var act = () => _resolver.GetMapping<TestEntityWithoutDocument>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No mapping registered for entity type TestEntityWithoutDocument");
    }
    
    private class TestEntityWithoutDocument : IDomainEntity
    {
        public Guid Id { get; set; }
    }
}
