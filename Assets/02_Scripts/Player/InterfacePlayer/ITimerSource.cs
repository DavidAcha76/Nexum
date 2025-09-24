public interface ITimeSource
{
    float DeltaTime { get; }
    float FixedDeltaTime { get; }
}
