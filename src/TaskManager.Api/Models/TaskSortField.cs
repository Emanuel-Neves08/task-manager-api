using System.Text.Json.Serialization;

namespace TaskManager.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskSortField
{
    CreatedAt = 0,
    UpdatedAt = 1,
    DueDate = 2,
    Priority = 3,
    Status = 4,
    Title = 5
}
