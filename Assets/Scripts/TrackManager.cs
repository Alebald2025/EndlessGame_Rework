using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackManager : MonoBehaviour
{
    [Header("Prefabs de Pistas")]
    [Tooltip("El primer elemento (Índice 0) debe ser el segmento vacío/inicio sin obstáculos.")]
    [SerializeField] private GameObject[] tilePrefabs;

    [Header("Configuración del Camino")]
    [Tooltip("Longitud de cada segmento en el eje Z.")]
    [SerializeField] private float tileLength = 30f;
    [Tooltip("Número de segmentos visibles en pantalla al mismo tiempo.")]
    [SerializeField] private int numberOfTiles = 5;
    [Tooltip("Distancia detrás del jugador antes de que un segmento sea destruido.")]
    [SerializeField] private float safeZone = 35f;

    [Header("Referencia del Jugador")]
    [SerializeField] private Transform playerTransform;

    private List<GameObject> activeTiles = new List<GameObject>();
    private float spawnZ = 0f;

    private void Start()
    {
        if (playerTransform == null)
        {
            // Intentar buscar al jugador si no está asignado
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // Generar los segmentos iniciales
        for (int i = 0; i < numberOfTiles; i++)
        {
            if (i < 1)
            {
                // Instanciar el primer segmento vacío para dar tiempo de reacción
                SpawnTile(0);
            }
            else
            {
                // Instanciar segmentos con obstáculos aleatorios (excluyendo el tile de inicio vacío si hay otros disponibles)
                SpawnTile(tilePrefabs.Length > 1 ? Random.Range(1, tilePrefabs.Length) : 0);
            }
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Comprobar si el jugador avanzó lo suficiente para generar una pista y eliminar la anterior
        if (playerTransform.position.z - safeZone > (spawnZ - numberOfTiles * tileLength))
        {
            // Crear una nueva pista al azar (excluyendo el tile de inicio vacío si hay otros disponibles)
            int randomPrefabIndex = tilePrefabs.Length > 1 ? Random.Range(1, tilePrefabs.Length) : 0;
            SpawnTile(randomPrefabIndex);
            
            // Eliminar la pista que quedó más atrás
            DeleteTile();
        }
    }

    private void SpawnTile(int prefabIndex)
    {
        if (tilePrefabs == null || tilePrefabs.Length == 0)
        {
            Debug.LogError("[TrackManager] ¡Faltan asignar los prefabs de las pistas en el Inspector!");
            return;
        }

        // Asegurar que el índice esté dentro del rango
        prefabIndex = Mathf.Clamp(prefabIndex, 0, tilePrefabs.Length - 1);

        // Instanciar el prefab en la posición actual de spawnZ
        GameObject tile = Instantiate(tilePrefabs[prefabIndex], transform.forward * spawnZ, Quaternion.identity);
        
        // Emparentar con este objeto para mantener la jerarquía limpia
        tile.transform.SetParent(transform);
        
        // Agregar a la lista de segmentos activos
        activeTiles.Add(tile);
        
        // Incrementar el punto de spawn para el siguiente tile
        spawnZ += tileLength;
    }

    private void DeleteTile()
    {
        if (activeTiles.Count > 0)
        {
            Destroy(activeTiles[0]);
            activeTiles.RemoveAt(0);
        }
    }

    // Método para reiniciar el circuito cuando el jugador vuelve a jugar
    public void ResetTrack()
    {
        // Limpiar todas las pistas activas
        foreach (GameObject tile in activeTiles)
        {
            Destroy(tile);
        }
        activeTiles.Clear();

        // Resetear spawnZ
        spawnZ = 0f;

        // Volver a spawnear las pistas iniciales
        for (int i = 0; i < numberOfTiles; i++)
        {
            if (i < 1)
            {
                SpawnTile(0);
            }
            else
            {
                SpawnTile(tilePrefabs.Length > 1 ? Random.Range(1, tilePrefabs.Length) : 0);
            }
        }
    }
}
