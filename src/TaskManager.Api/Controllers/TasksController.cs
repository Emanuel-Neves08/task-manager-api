using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Dtos;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController(ITaskService tasks) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskResponse>>> GetAll(
        [FromQuery] TaskQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await tasks.GetAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return TaskNotFound(id);
        }

        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var created = await tasks.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskResponse>> Update(
        int id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await tasks.UpdateAsync(id, request, cancellationToken);
        if (updated is null)
        {
            return TaskNotFound(id);
        }

        return Ok(updated);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<TaskResponse>> UpdateStatus(
        int id,
        [FromBody] UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await tasks.UpdateStatusAsync(id, request.Status, cancellationToken);
        if (updated is null)
        {
            return TaskNotFound(id);
        }

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await tasks.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return TaskNotFound(id);
        }

        return NoContent();
    }

    private NotFoundObjectResult TaskNotFound(int id) =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Task not found",
            Detail = $"No task was found with id {id}."
        });
}
