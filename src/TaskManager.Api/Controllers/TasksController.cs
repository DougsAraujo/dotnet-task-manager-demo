using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Security;
using TaskManager.Application.Contracts;
using TaskManager.Application.Services;

namespace TaskManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class TasksController(TaskItemService taskService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskItemResponse>>> List(CancellationToken cancellationToken)
    {
        var userId = CurrentUser.GetUserId(User);
        var items = await taskService.ListAsync(userId, cancellationToken).ConfigureAwait(false);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskItemResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.GetUserId(User);
        var item = await taskService.GetAsync(userId, id, cancellationToken).ConfigureAwait(false);
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TaskItemResponse>> Create([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.GetUserId(User);
        var created = await taskService.CreateAsync(userId, request, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskItemResponse>> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.GetUserId(User);
        var updated = await taskService.UpdateAsync(userId, id, request, cancellationToken).ConfigureAwait(false);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.GetUserId(User);
        await taskService.DeleteAsync(userId, id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
