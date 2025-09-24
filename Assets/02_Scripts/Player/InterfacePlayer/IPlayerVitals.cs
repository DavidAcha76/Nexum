public interface IPlayerVitals
{
    /// <summary> Salud normalizada 0..1. </summary>
    float Health01 { get; }

    /// <summary> Estamina normalizada 0..1. </summary>
    float Stamina01 { get; }
}
