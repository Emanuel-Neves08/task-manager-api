using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Dtos;
using TaskManager.Api.Mappers;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public class TaskService(AppDbContext db, TimeProvider clock) : ITaskService
{
    public async Task<PagedResult<TaskResponse>> GetAsync(TaskQuery filter, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var query = db.Tasks.AsNoTracking();

        if (filter.Status is { } status)
        {
            query = query.Where(task => task.Status == status);
        }

        if (filter.Priority is { } priority)
        {
            query = query.Where(task => task.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(task => task.Title.Contains(term)
                || (task.Description != null && task.Description.Contains(term)));
        }

        if (filter.DueBefore is { } dueBefore)
        {
            query = query.Where(task => task.DueDate != null && task.DueDate <= dueBefore);
        }

        if (filter.DueAfter is { } dueAfter)
        {
            query = query.Where(task => task.DueDate != null && task.DueDate >= dueAfter);
        }

        if (filter.Overdue == true)
        {
            query = query.Where(task => task.DueDate != null
                && task.DueDate < now
                && task.Status != TaskItemStatus.Completed);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var tasks = await Sort(query, filter.SortBy, filter.Descending)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);
        var items = tasks.Select(task => task.ToResponse(now)).ToList();

        return new PagedResult<TaskResponse>(items, filter.Page, filter.PageSize, totalCount, totalPages);
    }

    public async Task<TaskResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        return task?.ToResponse(clock.GetUtcNow());
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = NormalizeDescription(request.Description),
            Status = TaskItemStatus.Pending,
            Priority = request.Priority,
            DueDate = request.DueDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        return task.ToResponse(now);
    }

    public async Task<TaskResponse?> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var now = clock.GetUtcNow();

        task.Title = request.Title.Trim();
        task.Description = NormalizeDescription(request.Description);
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        ApplyStatus(task, request.Status, now);
        task.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        return task.ToResponse(now);
    }

    public async Task<TaskResponse?> UpdateStatusAsync(int id, TaskItemStatus status, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var now = clock.GetUtcNow();

        ApplyStatus(task, status, now);
        task.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        return task.ToResponse(now);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return false;
        }

        db.Tasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static void ApplyStatus(TaskItem task, TaskItemStatus status, DateTimeOffset now)
    {
        if (status == TaskItemStatus.Completed)
        {
            task.CompletedAt = task.Status == TaskItemStatus.Completed ? task.CompletedAt ?? now : now;
        }
        else
        {
            task.CompletedAt = null;
        }

        task.Status = status;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static IOrderedQueryable<TaskItem> Sort(IQueryable<TaskItem> query, TaskSortField field, bool descending)
    {
        var ordered = field switch
        {
            TaskSortField.Title => ApplyOrder(query, task => task.Title, descending),
            TaskSortField.Status => ApplyOrder(query, task => task.Status, descending),
            TaskSortField.Priority => ApplyOrder(query, task => task.Priority, descending),
            TaskSortField.DueDate => ApplyOrder(query, task => task.DueDate, descending),
            TaskSortField.UpdatedAt => ApplyOrder(query, task => task.UpdatedAt, descending),
            _ => ApplyOrder(query, task => task.CreatedAt, descending)
        };

        return ordered.ThenBy(task => task.Id);
    }

    private static IOrderedQueryable<TaskItem> ApplyOrder<TKey>(
        IQueryable<TaskItem> query,
        Expression<Func<TaskItem, TKey>> key,
        bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);
}
