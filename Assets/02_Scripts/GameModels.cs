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
    public int Rarity { get; set; } // 1 com�n, 2 raro, 3 �pico, 4 legendario
}
