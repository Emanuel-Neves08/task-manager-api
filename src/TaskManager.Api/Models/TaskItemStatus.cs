using System.Text.Json.Serialization;

namespace TaskManager.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskItemStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2
}
