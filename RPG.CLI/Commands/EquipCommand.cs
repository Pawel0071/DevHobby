using System.CommandLine;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RPG.Application.Commands;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Domain.Enums;
using RPG.Infrastructure.Interfaces;

namespace RPG.CLI.Commands;

public class EquipCommand
{
    private readonly IMediator _mediator;
    private readonly IDictionaryRegistry<ItemTypeDefinition> _itemDefinitions;

    public EquipCommand(IServiceProvider provider)
    {
        _mediator = provider.GetRequiredService<IMediator>();
        _itemDefinitions = provider
            .GetRequiredService<IDictionaryRegistry<ItemTypeDefinition>>();
    }
    
    public Command Build()
    {
        var characterOption = new Option<Guid>("--character", "ID postaci") { IsRequired = true };
        var slotOption = new Option<EquipmentSlot>("--slot", "Slot ekwipunku") { IsRequired = true };
        var itemNameOption = new Option<string>("--item-name", "Nazwa przedmiotu") { IsRequired = true };

        var cmd = new Command("equip", "Wyposaża przedmiot")
        {
            characterOption,
            slotOption,
            itemNameOption
        };

        cmd.SetHandler(async (Guid characterId, EquipmentSlot slot, string itemName) =>
            {
                var item = new Item(Guid.NewGuid(), _itemDefinitions.Get("test")!.Code!);
                var command = new EquipItemCommand(characterId, slot, item);
                await _mediator.Send(command);
            },
            characterOption, slotOption, itemNameOption);

        return cmd;
    }
}