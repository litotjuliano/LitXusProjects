using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Shared.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public string Type { get; private set; } = string.Empty;

    private Notification() { }

    public static Notification Create(Guid userId, string title, string body, string type)
    {
        return new Notification { UserId = userId, Title = title, Body = body, Type = type };
    }

    public void MarkRead() => IsRead = true;
}
