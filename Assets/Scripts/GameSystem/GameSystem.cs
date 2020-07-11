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

    PlayerController player;

    public PlayerController Player
    {
        get
        {
            if(!player)
                player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
            return player;
        }
    }

    
    // Use this for initialization
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
    }

    public void Snap(int pid)
    {
        CameraManager.Instance.StopMotion();
    }

    public void UnSnap(int pid)
    {
        CameraManager.Instance.StartMotion();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            Snap(0);

        if (Input.GetKeyDown(KeyCode.F2))
            UnSnap(0);
    }
}
