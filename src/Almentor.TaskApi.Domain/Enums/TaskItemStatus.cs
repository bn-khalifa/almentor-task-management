namespace Almentor.TaskApi.Domain.Enums;

/*
   Lifecycle state of a task. Serialized to the API as todo, in_progress, or done,
   stored in the database as its string name for readability and stability.
*/
public enum TaskItemStatus
{
    Todo,
    InProgress,
    Done
}
