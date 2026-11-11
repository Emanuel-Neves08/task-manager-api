using System.ComponentModel.DataAnnotations;
using TaskManager.Api.Models;

namespace TaskManager.Api.Dtos;

public class TaskQuery
{
    [EnumDataType(typeof(TaskItemStatus))]
    public TaskItemStatus? Status { get; set; }

    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority? Priority { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }

    public DateTimeOffset? DueBefore { get; set; }

    public DateTimeOffset? DueAfter { get; set; }

    public bool? Overdue { get; set; }

    [EnumDataType(typeof(TaskSortField))]
    public TaskSortField SortBy { get; set; } = TaskSortField.CreatedAt;

    public bool Descending { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
