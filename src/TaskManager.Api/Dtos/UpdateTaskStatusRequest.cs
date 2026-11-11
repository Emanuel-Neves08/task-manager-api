using System.ComponentModel.DataAnnotations;
using TaskManager.Api.Models;

namespace TaskManager.Api.Dtos;

public class UpdateTaskStatusRequest
{
    [EnumDataType(typeof(TaskItemStatus))]
    public TaskItemStatus Status { get; set; }
}
