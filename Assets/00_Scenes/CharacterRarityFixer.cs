// Guarda este archivo bajo Assets/Editor/CharacterRarityFixer.cs

#if UNITY_EDITOR

using UnityEngine;

using UnityEditor;

using System.IO;

using System.Linq;

using SQLite;

/// <summary>

/// Herramienta de editor para dejar la tabla Character con estos datos fijos:

/// 1 – Joshu   (3★)

/// 2 – Kobeni  (4★)

/// 3 – Spike   (4★)

/// 4 – Teniente(4★)

/// 5 – Ada     (4★)

/// 6 – Tracer  (5★)

/// 7 – Lina    (5★)

/// </summary>

public class CharacterRarityFixer : EditorWindow

{

    [MenuItem("Tools/Fix Character Rarities")]

    public static void FixRarities()

    {

        string dbPath = Path.Combine(Application.persistentDataPath, "game.db");

        if (!File.Exists(dbPath))

        {

            Debug.LogError($"[Fixer] No se encontró la BD en: {dbPath}");

            return;

        }

        var conn = new SQLiteConnection(dbPath);

        // Definimos el catálogo deseado

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

        // Leemos todo lo que hay ahora

        var all = conn.Table<Character>().ToList();

        int inserted = 0, updated = 0, removed = 0;

        // 1) Actualizar o insertar cada deseado

        foreach (var d in desired)

        {

            var existing = all.FirstOrDefault(x => x.Id == d.Id);

            if (existing == null)

            {

                // Insert con ID explícito: primero desactivar autoincrement

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

        // 2) Borrar cualquier otro ID fuera del rango 1–7

        var toRemove = all.Where(x => x.Id < 1 || x.Id > 7).ToList();

        foreach (var bad in toRemove)

        {

            conn.Delete(bad);

            removed++;

        }

        conn.Close();

        Debug.Log($"[Fixer] Insertados: {inserted}, Actualizados: {updated}, Borrados: {removed}");

    }

}

// Asegúrate de tener esta clase en tu proyecto (por ejemplo, en Scripts/Models/Character.cs)



#endif

