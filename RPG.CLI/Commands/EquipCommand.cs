
using System.CommandLine;
using System.CommandLine.Invocation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RPG.Domain.Enums;
using RPG.Domain.Entities;
using RPG.Application.Commands;
using RPG.Domain.Common;

namespace RPG.CLI.Commands;

public class EquipCommand
{
    private readonly IMediator _mediator;

    public EquipCommand(IServiceProvider provider)
    {
        _mediator = provider.GetRequiredService<IMediator>();
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
                var item = new Item
                {
                    Id = Guid.NewGuid(),
                    Name = itemName
                };

                var command = new EquipItemCommand(characterId, slot, item);
                await _mediator.Send(command);
            },
            characterOption, slotOption, itemNameOption);

        return cmd;
    }
}