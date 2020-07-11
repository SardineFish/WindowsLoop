using UnityEngine;
using System.Collections;

public class GameSystem : Singleton<GameSystem>
{
    [SerializeField]
    private float m_RenderTileSize = 40;

    [SerializeField]
    private float m_WorldTileSize = 1;

    public static float RenderTileSize => Instance?.m_RenderTileSize ?? 0;

    public static float WorldTileSize => Instance?.m_WorldTileSize ?? 0;
    
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
