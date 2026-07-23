namespace Almentor.TaskApi.Application.Common.Models;

public class PaginationParams
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;

    private int _offset;
    private int _limit = DefaultLimit;

    public int Offset
    {
        get => _offset;
        set => _offset = value < 0 ? 0 : value;
    }

    public int Limit
    {
        get => _limit;
        set => _limit = value switch
        {
            <= 0 => DefaultLimit,
            > MaxLimit => MaxLimit,
            _ => value
        };
    }
}
