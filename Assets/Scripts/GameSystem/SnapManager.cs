using UnityEngine;
using System.Collections;
using WindowSnap;
using System.Collections.Generic;

public class SnapManager : Singleton<SnapManager>
{
    Dictionary<int, GameInstanceData> pages = new Dictionary<int, GameInstanceData>();
    public GameInstanceData GetGameData(int pid)
    {
        if(!pages.ContainsKey(pid))
        {
            var page = WindowSnap.SharedMemory.GetPageByPID(pid);
            if (page != null)
                pages[pid] = new GameInstanceData(page);
        }
        return pages[pid];
    }
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
    }
    // Use this for initialization
    void Start()
    {

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
        
    }
}
