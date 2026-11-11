using System.Text.Json.Serialization;

namespace TaskManager.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}
