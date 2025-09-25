using UnityEngine;
using System.IO;
using System.Linq;
using SQLite;

/// <summary>
/// Este script asegura que la primera vez que se ejecute el juego:
/// - La tabla Character tenga los personajes iniciales.
/// - El jugador empiece con al menos 100 monedas.
/// - Luego no se vuelve a ejecutar automáticamente.
/// </summary>
public class InitialSetup : MonoBehaviour
{
    private void Start()
    {
        // Solo la primera vez
        if (!PlayerPrefs.HasKey("FirstLaunch"))
        {
            ConfigureDatabase();
            PlayerPrefs.SetInt("FirstLaunch", 1);
            PlayerPrefs.Save();

            Debug.Log("[InitialSetup] Base de datos inicializada por primera vez.");
        }
        else
        {
            Debug.Log("[InitialSetup] Ya se había inicializado previamente.");
        }
    }

    private void ConfigureDatabase()
    {
        string dbPath = Path.Combine(Application.persistentDataPath, "game.db");

        if (!File.Exists(dbPath))
        {
            Debug.LogError($"[InitialSetup] No se encontró la BD en: {dbPath}");
            return;
        }

        var conn = new SQLiteConnection(dbPath);

        // ================================
        // 1) Personajes iniciales
        // ================================
        var desired = new[]
        {
            new Character{ Id = 1, Name = "Joshu",    Rarity = 3 },
            new Character{ Id = 2, Name = "Kobeni",   Rarity = 4 },
            new Character{ Id = 3, Name = "Spike",    Rarity = 4 },
            new Character{ Id = 4, Name = "Teniente", Rarity = 4 },
            new Character{ Id = 5, Name = "Ada",      Rarity = 4 },
            new Character{ Id = 6, Name = "Tracer",   Rarity = 5 },
            new Character{ Id = 7, Name = "Lina",     Rarity = 5 },
        };

        var all = conn.Table<Character>().ToList();

        int inserted = 0, updated = 0, removed = 0;

        foreach (var d in desired)
        {
            var existing = all.FirstOrDefault(x => x.Id == d.Id);
            if (existing == null)
            {
                conn.Execute("INSERT INTO Character (Id, Name, Rarity) VALUES (?, ?, ?);",
                             d.Id, d.Name, d.Rarity);
                inserted++;
            }
            else
            {
                if (existing.Name != d.Name || existing.Rarity != d.Rarity)
                {
                    existing.Name = d.Name;
                    existing.Rarity = d.Rarity;
                    conn.Update(existing);
                    updated++;
                }
            }
        }

        // Eliminar personajes fuera de rango 1-7
        var toRemove = all.Where(x => x.Id < 1 || x.Id > 7).ToList();
        foreach (var bad in toRemove)
        {
            conn.Delete(bad);
            removed++;
        }

        Debug.Log($"[InitialSetup] Characters => Insertados: {inserted}, Actualizados: {updated}, Borrados: {removed}");

        // ================================
        // 2) Monedas iniciales para el jugador
        // ================================
        EnsurePlayerStartsWith100Coins(conn);

        conn.Close();
    }

    private void EnsurePlayerStartsWith100Coins(SQLiteConnection conn)
    {
        // Intentar crear la tabla si no existe
        try
        {
            conn.CreateTable<PlayerStats>();
        }
        catch
        {
            Debug.LogWarning("[InitialSetup] No se pudo crear/verificar la tabla PlayerStats.");
            return;
        }

        var player = conn.Table<PlayerStats>().FirstOrDefault(p => p.Id == 1);

        if (player == null)
        {
            // Insertar nuevo jugador con 100 monedas
            conn.Insert(new PlayerStats { Id = 1, Coins = 100 });
            Debug.Log("[InitialSetup] Jugador creado con 100 monedas.");
        }
        else
        {
            // Si tiene menos de 100 monedas, subirlo a 100
            if (player.Coins < 100)
            {
                player.Coins = 100;
                conn.Update(player);
                Debug.Log("[InitialSetup] Monedas actualizadas a 100.");
            }
            else
            {
                Debug.Log("[InitialSetup] El jugador ya tiene 100 o más monedas.");
            }
        }
    }
}

/// <summary>
/// Modelo de personajes.
/// </summary>


/// <summary>
/// Modelo de estadísticas del jugador.
/// </summary>
public class PlayerStats
{
    [PrimaryKey]
    public int Id { get; set; }
    public int Coins { get; set; }
}
