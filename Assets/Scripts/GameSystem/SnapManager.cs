using UnityEngine;
using System.Collections;
using WindowSnap;
using System.Collections.Generic;
using System.Linq;

public class SnapManager : Singleton<SnapManager>
{

    public GameInstanceData GetGameData(int pid) => new GameInstanceData(SharedMemory.GetPageByPID(pid));
    GameInstanceData SelfData;
    public int PID { get; private set; }
    Dictionary<int, AttachedInstanceData> AttachedData = new Dictionary<int, AttachedInstanceData>();
    public HashSet<int> AttachedInstances = new HashSet<int>();
    private void Awake()
    {
        Snapper.OnAttached += Snapper_OnAttached;
        Snapper.OnDetached += Snapper_OnDetached;
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

        PID = Snapper.PID;

    }

    private void Snapper_OnDetached(int pid)
    {
        AttachedInstances.Remove(pid);
        if(AttachedInstances.Count == 0 && PublicData.ActiveInstancePID == Snapper.PID)
        {
            CameraManager.Instance.StartMotion();
        }
    }

    private void Snapper_OnAttached(int pid, Vec2 relativePos)
    {
        Vector2Int posInt = new Vector2Int(Mathf.RoundToInt(relativePos.X / 40), Mathf.RoundToInt(relativePos.Y / 40));

        if (!AttachedData.ContainsKey(pid))
        {
            var data = GetGameData(pid);
            if(data.Valid)
            {
                var relativeRect = data.ViewRect;
                var tileData = new AttachedInstanceData()
                {
                    PID = pid,
                    TileRange = data.TileRange,
                    TileData = data.ReadTileData(),
                    ViewportTileRect = data.ViewRect,
                };
                AttachedData[pid] = tileData;
            }
        }
        if(AttachedData.ContainsKey(pid))
        {
            var instanceData = GetGameData(pid);
            var attachedData = AttachedData[pid];
            attachedData.AttachPoint = posInt;
            attachedData.AttachPoint.y = -posInt.y;

            RectInt relativeViewRect = new RectInt();
            relativeViewRect.min = attachedData.AttachPoint;
            relativeViewRect.max = attachedData.AttachPoint + attachedData.ViewportTileRect.size;
            attachedData.RelativeTileRect = relativeViewRect;

            var viewportBlocks = CameraManager.Instance.ViewportTileRect;

            RectInt syncRange = new RectInt();
            if (posInt.x == -viewportBlocks.size.x) // snap to left
            {
                syncRange.xMin = -CameraManager.Instance.preloadExtend;
                syncRange.xMax = 0;
                syncRange.yMin = Mathf.Max(0, attachedData.AttachPoint.y);
                syncRange.yMax = Mathf.Min(viewportBlocks.size.y, attachedData.AttachPoint.y + viewportBlocks.size.y);
            }
            else if (posInt.x == viewportBlocks.size.x) // snap to right
            {
                syncRange.xMin = viewportBlocks.size.x;
                syncRange.xMax = viewportBlocks.size.x + CameraManager.Instance.preloadExtend;
                syncRange.yMin = Mathf.Max(0, attachedData.AttachPoint.y);
                syncRange.yMax = Mathf.Min(viewportBlocks.size.y, attachedData.AttachPoint.y + viewportBlocks.size.y);
            }
            else if (posInt.y == viewportBlocks.size.y) // snap to bottom
            {
                syncRange.xMin = Mathf.Max(0, attachedData.AttachPoint.x);
                syncRange.xMax = Mathf.Min(viewportBlocks.size.x, attachedData.AttachPoint.x + viewportBlocks.size.x);
                syncRange.yMin = -CameraManager.Instance.preloadExtend;
                syncRange.yMax = 0;
            }
            else if (posInt.y == -viewportBlocks.size.y) // snap to top
            {
                syncRange.xMin = Mathf.Max(0, attachedData.AttachPoint.x);
                syncRange.xMax = Mathf.Min(viewportBlocks.size.x, attachedData.AttachPoint.x + viewportBlocks.size.x);
                syncRange.yMin = viewportBlocks.size.y;
                syncRange.yMax = viewportBlocks.size.y + CameraManager.Instance.preloadExtend;
            }

            attachedData.SyncTilesArea = syncRange;
            var min = syncRange.min + viewportBlocks.min;
            var max = syncRange.max + viewportBlocks.min;
            syncRange.min = min;
            syncRange.max = max;

            var instanceViewport = instanceData.ViewRect;

            for(int x = syncRange.xMin; x < syncRange.xMax; ++x)
            {
                for(int y = syncRange.yMin; y < syncRange.yMax; ++y)
                {
                    var pos = new Vector2Int(x, y);
                    pos -= viewportBlocks.min;
                    pos -= attachedData.AttachPoint;
                    pos += instanceViewport.min;

                    var tile = attachedData.GetTileAt(pos);
                    GameMap.Instance.SetAttachedTile(new Vector2Int(x, y), tile);
                }
            }

            AttachedInstances.Add(pid);
            if(PublicData.ActiveInstancePID == pid)
            {
                SelfData.ConnectActiveThrough = pid;
            }
            else if(instanceData.ConnectActiveThrough != 0)
            {
                SelfData.ConnectActiveThrough = pid;
            }
            SelfData.Flush();

            Debug.LogError($"{syncRange.min - viewportBlocks.min}, {syncRange.max - viewportBlocks.min}");
        }

        CameraManager.Instance.StopMotion();
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
                    tiles[x, y] = AssetManager.Instance.GetID(tile);
                }
                else
                    tiles[x, y] = -1;
            }
        }
        SelfData.TileRange = rect;
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
        SelfData.ViewRect = CameraManager.Instance.ViewportTileRect;
        SelfData.Flush();

        bool shouldUpdateConnection = true;
        var connectActiveInstanceThrough = SelfData.ConnectActiveThrough;
        if (connectActiveInstanceThrough != 0 && AttachedInstances.Contains(connectActiveInstanceThrough))
        {
            var instanceData = GetGameData(connectActiveInstanceThrough);
            if(instanceData.ConnectActiveThrough != 0 || PublicData.ActiveInstancePID == connectActiveInstanceThrough)
            {
                shouldUpdateConnection = false;
            }
        }

        // Update connection to active instance by searching all attached instance
        // Thees values will propergate to all attached game instance.
        if (shouldUpdateConnection)
        {
            var activePID = PublicData.ActiveInstancePID;
            SelfData.ConnectActiveThrough = 0;

            foreach (var attached in AttachedInstances)
            {
                var instanceData = GetGameData(attached);
                if (attached == activePID)
                {
                    SelfData.ConnectActiveThrough = attached;
                    break;
                }
                else if(instanceData.ConnectActiveThrough != 0 && instanceData.ConnectActiveThrough != PID)
                {
                    SelfData.ConnectActiveThrough = attached;
                    break;
                }
            }
        }
        SelfData.Flush();

        var connectThrough = SelfData.ConnectActiveThrough;
        if (connectThrough != 0)
        {
            var instanceData = GetGameData(connectThrough);
            var offset = -AttachedData[connectThrough].AttachPoint;
            if(connectThrough == PublicData.ActiveInstancePID)
            {
                SelfData.TileOffsetToActiveInstance = offset;
            }
            else
            {
                SelfData.TileOffsetToActiveInstance = instanceData.TileOffsetToActiveInstance + offset;
            }
        }
    }

    private void LateUpdate()
    {
        if(SelfData.IsActiveInstance)
        {
            GameSystem.Instance.Player.gameObject.SetActive(true);
            GameSystem.Instance.Player.EnableControl = true;

            var viewportRect = CameraManager.Instance.ViewportTileRect;
            var pos = GameSystem.Instance.Player.transform.position.ToVector2();
            var viewportTilePos = GameSystem.Instance.Player.transform.position - viewportRect.min.ToVector3();
            if(! viewportRect.Contains(pos))
            {
                var relativePos = pos - viewportRect.min;

                var nextActive = AttachedData
                    .Where(attachedData => attachedData.Value.RelativeTileRect.Contains(relativePos))
                    .Select(p => p.Value)
                    .FirstOrDefault();

                if(nextActive is null)
                    Debug.LogError($"Missing attached instance contains {pos}");
                else
                {
                    var instanceData = GetGameData(nextActive.PID);

                    instanceData.IsActiveInstance = true;
                    instanceData.Flush();

                    PublicData.ActiveInstancePID = nextActive.PID;
                    PublicData.Flush();

                    SelfData.IsActiveInstance = false;
                    SelfData.Flush();

                }

            }
        }
        else if(SelfData.ConnectActiveThrough != 0)
        {
            GameSystem.Instance.Player.gameObject.SetActive(true);
            var activePID = PublicData.ActiveInstancePID;
            var activeInstance = GetGameData(activePID);

            var pos = activeInstance.PlayerPosition;
            var velocity = activeInstance.PlayerVelocity;

            pos -= activeInstance.ViewRect.min;
            pos -= SelfData.TileOffsetToActiveInstance;
            pos += CameraManager.Instance.ViewportTileRect.min;

            SelfData.PlayerPosition = pos;
            SelfData.PlayerVelocity = velocity;
            SelfData.Flush();

            GameSystem.Instance.Player.SetPositionVelocity(pos, velocity);

            //Debug.LogError($"Sync from active instance {activePID}");
        }
        else
        {
            GameSystem.Instance.Player.gameObject.SetActive(false);
        }
    }
}
