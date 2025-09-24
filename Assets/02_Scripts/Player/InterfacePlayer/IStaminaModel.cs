// IStaminaModel.cs
public interface IStaminaModel
{
    float Max { get; set; }
    float Current { get; set; }
    float RegenPerSecond { get; set; }
    float RegenDelay { get; set; }
    float SprintDrainPerSecond { get; set; }
    float MinSpeedAtZeroStamina { get; set; }
    void Tick(bool moving, bool sprinting, float dt);
    bool CanSprint(float minSprintStamina);
}
