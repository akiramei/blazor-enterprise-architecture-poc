using System.Text.Json;

namespace Shared.Domain.Idempotency;

/// <summary>
/// 冪等性レコード
/// </summary>
public sealed class IdempotencyRecord
{
    public string Key { get; private set; } = default!;
    public string CommandType { get; private set; } = default!;
    public string ResultJson { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private IdempotencyRecord()
    {
        // EF Core用のプライベートコンストラクタ
    }

    public static IdempotencyRecord Create<TResponse>(
        string key,
        string commandType,
        TResponse result)
    {
        return new IdempotencyRecord
        {
            Key = key,
            CommandType = commandType,
            ResultJson = JsonSerializer.Serialize(result),
            CreatedAt = DateTime.UtcNow
        };
    }

    public TResponse GetResult<TResponse>()
    {
        return JsonSerializer.Deserialize<TResponse>(ResultJson)!;
    }
}
