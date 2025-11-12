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

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IModelMapper<Character, CharacterDocument>)))
            .Returns(new Mock<IModelMapper<Character, CharacterDocument>>().Object);

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IModelMapper<Item, ItemDocument>)))
            .Returns(new Mock<IModelMapper<Item, ItemDocument>>().Object);

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
        mapper.Should().BeAssignableTo<IModelMapper<Character, CharacterDocument>>();
    }

    [Fact]
    public void GetMapping_ForItem_ReturnsItemDocumentAndMapper()
    {
        // Act
        var (documentType, mapper) = _resolver.GetMapping<Item>();

        // Assert
        documentType.Should().Be(typeof(ItemDocument));
        mapper.Should().NotBeNull();
        mapper.Should().BeAssignableTo<IModelMapper<Item, ItemDocument>>();
    }

    [Fact]
    public void GetMapping_ForNonExistentDocument_ThrowsException()
    {
        // Act & Assert
        var act = () => _resolver.GetMapping<TestModelWithoutDocument>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No mapping registered for entity type TestModelWithoutDocument");
    }

    private class TestModelWithoutDocument : IDomainModel
    {
        public Guid Id { get; set; }
    }
}
