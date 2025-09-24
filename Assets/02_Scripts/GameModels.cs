using SQLite;

public class Player
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int Gold { get; set; }
}

public class Character
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public int Rarity { get; set; } // 3, 4 o 5
}

public class OwnedCharacter
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public int Rarity { get; set; }
    public int Count { get; set; }   // cuántas copias posee el jugador
}
