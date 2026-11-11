using TaskManager.Api.Dtos;
using TaskManager.Api.Models;

namespace TaskManager.Api.Mappers;

public static class TaskMapper
{
    public static TaskResponse ToResponse(this TaskItem task, DateTimeOffset now)
    {
        var isOverdue = task.DueDate.HasValue
            && task.DueDate.Value < now
            && task.Status != TaskItemStatus.Completed;

        return new TaskResponse(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.DueDate,
            isOverdue,
            task.CreatedAt,
            task.UpdatedAt,
            task.CompletedAt);
    }
}
