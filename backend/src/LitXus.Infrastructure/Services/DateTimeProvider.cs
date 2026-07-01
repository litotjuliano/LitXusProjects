using LitXus.Application.Common.Interfaces;

namespace LitXus.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
