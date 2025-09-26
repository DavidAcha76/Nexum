using SQLite;

public class Player
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Oro actual del jugador
    public int Gold { get; set; }

    // Nuevo: Id del personaje seleccionado
    public int SelectedCharacterId { get; set; }
}

public class Character
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Nombre del personaje base
    public string Name { get; set; }

    // Rareza (3, 4 o 5 estrellas)
    public int Rarity { get; set; }
}

public class OwnedCharacter
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }          // Id del registro en la tabla de Owned

    // Referencia al Character base
    public int CharacterId { get; set; }

    public string Name { get; set; }
    public int Rarity { get; set; }

    // Cuántas copias posee el jugador
    public int Count { get; set; }
}
