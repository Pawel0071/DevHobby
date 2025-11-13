// filepath: /Volumes/Data/Repositories/DevHobby/RPG.GameServer/Controllers/QueryController.cs
using Microsoft.AspNetCore.Mvc;
using RPG.Application.Interfaces;
using RPG.Application.Queries;
using RPG.GameServer.Dtos;
using RPG.GameServer.Mappers;

namespace RPG.GameServer.Controllers;

[ApiController]
[Route("api/query")]
public class QueryController : ControllerBase
{
    private readonly IQueryBus _queryBus;
    private readonly RPG.Infrastructure.Interfaces.IModelRepository _repo;

    public QueryController(IQueryBus queryBus, RPG.Infrastructure.Interfaces.IModelRepository repo)
    {
        _queryBus = queryBus;
        _repo = repo;
    }

    [HttpGet("items/{id:guid}")]
    public async Task<ActionResult<ItemDto>> GetItem(Guid id, CancellationToken ct)
    {
        var item = await _repo.GetByIdAsync<RPG.Domain.Models.Items.Item>(id, ct);
        if (item is null) return NotFound();
        return Ok(item.ToDto());
    }

    [HttpGet("items")]
    public async Task<ActionResult<IReadOnlyList<ItemDto>>> GetItems(CancellationToken ct)
    {
        var items = await _repo.GetAllAsync<RPG.Domain.Models.Items.Item>(ct);
        return Ok(items.Select(i => i.ToDto()).ToList());
    }

    [HttpGet("skills/{id:guid}")]
    public async Task<ActionResult<SkillDto>> GetSkill(Guid id, CancellationToken ct)
    {
        var skill = await _repo.GetByIdAsync<RPG.Domain.Models.Skills.Skill>(id, ct);
        if (skill is null) return NotFound();
        return Ok(skill.ToDto());
    }

    [HttpGet("skills")]
    public async Task<ActionResult<IReadOnlyList<SkillDto>>> GetSkills(CancellationToken ct)
    {
        var skills = await _repo.GetAllAsync<RPG.Domain.Models.Skills.Skill>(ct);
        return Ok(skills.Select(s => s.ToDto()).ToList());
    }

    [HttpGet("npcs/{id:guid}")]
    public async Task<ActionResult<NpcDto>> GetNpc(Guid id, CancellationToken ct)
    {
        var npc = await _repo.GetByIdAsync<RPG.Domain.Models.Npcs.Npc>(id, ct);
        if (npc is null) return NotFound();
        return Ok(npc.ToDto());
    }

    [HttpGet("npcs")]
    public async Task<ActionResult<IReadOnlyList<NpcDto>>> GetNpcs(CancellationToken ct)
    {
        var npcs = await _repo.GetAllAsync<RPG.Domain.Models.Npcs.Npc>(ct);
        return Ok(npcs.Select(n => n.ToDto()).ToList());
    }
}
