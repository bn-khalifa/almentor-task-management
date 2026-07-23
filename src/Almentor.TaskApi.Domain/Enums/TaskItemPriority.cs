namespace Almentor.TaskApi.Domain.Enums;

/* 
   Priority of a task. Serialized to the API as low, medium, or high, stored in the database as its string name.
*/
public enum TaskItemPriority
{
    Low,
    Medium,
    High
}
