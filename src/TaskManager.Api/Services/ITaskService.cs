using TaskManager.Api.Dtos;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public interface ITaskService
{
    Task<PagedResult<TaskResponse>> GetAsync(TaskQuery filter, CancellationToken cancellationToken);

    Task<TaskResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken);

    Task<TaskResponse?> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken);

    Task<TaskResponse?> UpdateStatusAsync(int id, TaskItemStatus status, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
