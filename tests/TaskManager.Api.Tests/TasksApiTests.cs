using System.Net;
using System.Net.Http.Json;
using TaskManager.Api.Dtos;
using TaskManager.Api.Models;

namespace TaskManager.Api.Tests;

public class TasksApiTests(TaskManagerFactory factory) : IClassFixture<TaskManagerFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Create_ReturnsCreatedTaskWithDefaults()
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest
        {
            Title = "  Write report  ",
            Priority = TaskPriority.High
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(task);
        Assert.True(task.Id > 0);
        Assert.Equal("Write report", task.Title);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Null(task.CompletedAt);
        Assert.False(task.IsOverdue);
    }

    [Fact]
    public async Task Create_WithEmptyTitle_ReturnsBadRequest()
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest { Title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsTask_AndNotFoundForUnknownId()
    {
        var created = await CreateTaskAsync("Read a book");

        var found = await client.GetAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);

        var missing = await client.GetAsync("/api/tasks/999999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Update_ReplacesAllEditableFields()
    {
        var created = await CreateTaskAsync("Original title");
        var dueDate = DateTimeOffset.UtcNow.AddDays(3);

        var response = await client.PutAsJsonAsync($"/api/tasks/{created.Id}", new UpdateTaskRequest
        {
            Title = "Updated title",
            Description = "Updated description",
            Status = TaskItemStatus.InProgress,
            Priority = TaskPriority.Low,
            DueDate = dueDate
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Updated title", updated.Title);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(TaskItemStatus.InProgress, updated.Status);
        Assert.Equal(TaskPriority.Low, updated.Priority);
        Assert.NotNull(updated.DueDate);
    }

    [Fact]
    public async Task Update_UnknownTask_ReturnsNotFound()
    {
        var response = await client.PutAsJsonAsync("/api/tasks/999999", new UpdateTaskRequest { Title = "Nothing" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_CompletingSetsCompletedAt_AndReopeningClearsIt()
    {
        var created = await CreateTaskAsync("Finish homework");

        var completed = await PatchStatusAsync(created.Id, TaskItemStatus.Completed);
        Assert.Equal(TaskItemStatus.Completed, completed.Status);
        Assert.NotNull(completed.CompletedAt);

        var reopened = await PatchStatusAsync(created.Id, TaskItemStatus.Pending);
        Assert.Equal(TaskItemStatus.Pending, reopened.Status);
        Assert.Null(reopened.CompletedAt);
    }

    [Fact]
    public async Task Delete_RemovesTask()
    {
        var created = await CreateTaskAsync("Temporary task");

        var deleted = await client.DeleteAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);

        var deletedAgain = await client.DeleteAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deletedAgain.StatusCode);
    }

    [Fact]
    public async Task Overdue_TaskWithPastDueDateIsFlaggedUntilCompleted()
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest
        {
            Title = "Late task",
            DueDate = DateTimeOffset.UtcNow.AddDays(-2)
        });
        var created = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(created);
        Assert.True(created.IsOverdue);

        var completed = await PatchStatusAsync(created.Id, TaskItemStatus.Completed);
        Assert.False(completed.IsOverdue);
    }

    [Fact]
    public async Task List_SearchesFiltersSortsAndPaginates()
    {
        var marker = Guid.NewGuid().ToString("N");

        await CreateTaskAsync($"{marker} alpha", TaskPriority.Low);
        await CreateTaskAsync($"{marker} bravo", TaskPriority.High);
        var charlie = await CreateTaskAsync($"{marker} charlie", TaskPriority.Medium);
        await PatchStatusAsync(charlie.Id, TaskItemStatus.Completed);

        var firstPage = await client.GetFromJsonAsync<PagedResult<TaskResponse>>(
            $"/api/tasks?search={marker}&sortBy=Title&descending=false&page=1&pageSize=2");
        Assert.NotNull(firstPage);
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.EndsWith("alpha", firstPage.Items[0].Title);
        Assert.EndsWith("bravo", firstPage.Items[1].Title);

        var secondPage = await client.GetFromJsonAsync<PagedResult<TaskResponse>>(
            $"/api/tasks?search={marker}&sortBy=Title&descending=false&page=2&pageSize=2");
        Assert.NotNull(secondPage);
        Assert.Single(secondPage.Items);

        var completedOnly = await client.GetFromJsonAsync<PagedResult<TaskResponse>>(
            $"/api/tasks?search={marker}&status=Completed");
        Assert.NotNull(completedOnly);
        Assert.Single(completedOnly.Items);

        var highPriority = await client.GetFromJsonAsync<PagedResult<TaskResponse>>(
            $"/api/tasks?search={marker}&priority=High");
        Assert.NotNull(highPriority);
        Assert.Single(highPriority.Items);
    }

    [Fact]
    public async Task List_WithInvalidPaging_ReturnsBadRequest()
    {
        var response = await client.GetAsync("/api/tasks?page=0&pageSize=500");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<TaskResponse> CreateTaskAsync(string title, TaskPriority priority = TaskPriority.Medium)
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest
        {
            Title = title,
            Priority = priority
        });
        response.EnsureSuccessStatusCode();

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(task);
        return task;
    }

    private async Task<TaskResponse> PatchStatusAsync(int id, TaskItemStatus status)
    {
        var response = await client.PatchAsync(
            $"/api/tasks/{id}/status",
            JsonContent.Create(new UpdateTaskStatusRequest { Status = status }));
        response.EnsureSuccessStatusCode();

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(task);
        return task;
    }
}
