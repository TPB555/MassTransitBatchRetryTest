using MassTransit;

namespace MassTransitBatchRetryTest;

public class HyphenatedEntityNameFormatter : IEntityNameFormatter
{
    private readonly IEntityNameFormatter _original;

    public HyphenatedEntityNameFormatter(IEntityNameFormatter original)
    {
        _original = original;
    }

    public string FormatEntityName<T>()
    {
        string name = _original.FormatEntityName<T>();
        string output = name.Replace('.', '-').Replace(':', '-').ToLower();
        return output;
    }
}