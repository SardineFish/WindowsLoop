using UnityEngine;
using System.Collections;
using WindowSnap;
using System.Collections.Generic;
using System.Linq;

public class SnapManager : Singleton<SnapManager>
{
    public GameInstanceData GetGameData(int pid) => new GameInstanceData(SharedMemory.GetPageByPID(pid));
    GameInstanceData SelfData;
    private void Awake()
    {
        Snapper.OnAttachChanged += (targetPID, targetPos) =>
        {
            if (targetPID != 0)
                CameraManager.Instance.StopMotion();
            else
                CameraManager.Instance.StartMotion();
            Debug.LogError(targetPos);
        };
        Snapper.SnapWhileMoving = false;
        Snapper.SetLogCallback(msg =>
        {
            Debug.LogError(msg);
        });
        Snapper.Init();

        SelfData = new GameInstanceData(SharedMemory.Self);

        if(SharedMemory.Others.Count == 0)
        {
            SelfData.IsActiveInstance = true;

            PublicData.ActiveInstancePID = Snapper.PID;
            PublicData.Flush();
        }
        else
        {
            SelfData.IsActiveInstance = false;
        }

        SelfData.Flush();
        WriteTileData();
        SelfData.Flush();

    }

    void WriteTileData()
    {
        var rect = GameMap.Instance.LoopArea;

        var tiles = new int[rect.size.x, rect.size.y];

        var tilemap = GameMap.Instance;

        for(int x = 0; x < rect.size.x; ++x)
        {
            for(int y = 0; y < rect.size.y; ++y)
            {
                var pos = new Vector2Int(x + rect.xMin, y + rect.yMin);
                var tile = tilemap.GetBaseTileAt(pos);
                if (tile)
                {
                    tiles[x, y] = tile.GetInstanceID();
                }
                else
                    tiles[x, y] = 0;
            }
        }

        SelfData.WriteTileData(tiles);
    }

    // Use this for initialization
    void Start()
    {
        Debug.LogError(GameMap.Instance.GetBaseTileAt(new Vector2Int(5, 0)).GetInstanceID());
    }

    // Update is called once per frame
    void Update()
    {
        Snapper.TickPerFrame();

        SelfData.PlayerPosition = GameSystem.Instance.Player.transform.position;
        SelfData.PlayerVelocity = GameSystem.Instance.Player.rigidbody.velocity;
        SelfData.ViewRect = CameraManager.Instance.SnapRect;

        SelfData.Flush();
    }

    private void LateUpdate()
    {
        if(SelfData.IsActiveInstance)
        {
            GameSystem.Instance.Player.EnableControl = true;
        }
        
        else
        {
            var activePID = PublicData.ActiveInstancePID;
            var activeInstance = GetGameData(activePID);

            var pos = activeInstance.PlayerPosition;
            var velocity = activeInstance.PlayerVelocity;

            SelfData.PlayerPosition = pos;
            SelfData.PlayerVelocity = velocity;
            SelfData.Flush();

            GameSystem.Instance.Player.SetPositionVelocity(pos, velocity);

            Debug.LogError($"Sync from active instance {activePID}");
        }
    }
}
