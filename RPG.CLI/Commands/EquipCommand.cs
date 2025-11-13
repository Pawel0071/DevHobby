using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using RPG.Application.Commands;
using RPG.Application.Interfaces;
using RPG.Domain.Enums;
using RPG.Domain.Models.Items;

namespace RPG.CLI.Commands;

public class EquipCommand
{
    private readonly IServiceProvider _provider;

    public EquipCommand(IServiceProvider provider)
    {
        _provider = provider;
    }

    public Command Build()
    {
        var characterOption = new Option<Guid>("--character", "ID postaci") { IsRequired = true };
        var slotOption = new Option<EquipmentSlot>("--slot", "Slot ekwipunku") { IsRequired = true };
        var itemNameOption = new Option<string>("--item-name", "Nazwa przedmiotu") { IsRequired = true };

        var cmd = new Command("equip", "Wyposaża przedmiot") { characterOption, slotOption, itemNameOption };

        cmd.SetHandler(async (characterId, slot, itemName) =>
            {
                using var scope = _provider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<EquipItemCommand>>();

                var item = new Item(Guid.NewGuid(), itemName.ToLowerInvariant()) { Name = itemName };
                var command = new EquipItemCommand(characterId, slot, item);
                await handler.HandleAsync(command);
            },
            characterOption, slotOption, itemNameOption);

        return cmd;
    }
}
