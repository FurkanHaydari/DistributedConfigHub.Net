using DistributedConfigHub.Application.DTOs;
using DistributedConfigHub.Application.Features.Commands;
using DistributedConfigHub.Application.Features.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using DistributedConfigHub.Api.Filters;

namespace DistributedConfigHub.Api.Controllers;

[ApiController]
[Route("[controller]")]
[ServiceFilter(typeof(ApiKeyAuthorizeAttribute))]
public class ConfigurationsController(IMediator mediator) : ControllerBase
{
    private string GetCallerApp() => HttpContext.Items["CallerApplicationName"]?.ToString() ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConfigurationDto>>> GetConfigurations([FromQuery] string applicationName, [FromQuery] string? environment = null)
    {
        var result = await mediator.Send(new GetConfigurationsQuery(applicationName, environment, GetCallerApp()));
        return Ok(result);
    }

    [HttpGet("deleted")]
    public async Task<ActionResult<IEnumerable<ConfigurationDto>>> GetDeletedConfigurations([FromQuery] string applicationName, [FromQuery] string? environment = null)
    {
        var result = await mediator.Send(new GetDeletedConfigurationsQuery(applicationName, environment, GetCallerApp()));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConfigurationDto>> GetById(Guid id)
    {
        var result = await mediator.Send(new GetConfigurationByIdQuery(id, GetCallerApp()));
        if (result is null) return NotFound();
        return Ok(result);
    }
    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateConfigurationCommand command)
    {
        var secureCommand = command with { CallerApplicationName = GetCallerApp() };
        var id = await mediator.Send(secureCommand);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConfigurationCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch.");
        
        var secureCommand = new UpdateConfigurationCommand(id, command.Value, GetCallerApp());
        var success = await mediator.Send(secureCommand);
        if (!success) return NotFound();
        
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await mediator.Send(new DeleteConfigurationCommand(id, GetCallerApp()));
        if (!success) return NotFound();
        
        return NoContent();
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var history = await mediator.Send(new GetConfigurationHistoryQuery(id, GetCallerApp()));
        return Ok(history);
    }

    [HttpPost("{id:guid}/rollback/{auditLogId:guid}")]
    public async Task<IActionResult> Rollback(Guid id, Guid auditLogId)
    {
        var success = await mediator.Send(new RollbackConfigurationCommand(id, auditLogId, GetCallerApp()));
        if (!success) return BadRequest("Rollback failed. Either the configuration or the specified history log does not exist.");
        
        return Ok("Successfully rolled back the configuration.");
    }
}