using UnityEngine;
using System.Collections;
using WindowSnap;

public class SnapManager : Singleton<SnapManager>
{
    private void Awake()
    {
        Snapper.OnAttachChanged += (pid, pos) =>
        {
            if (pid != 0)
                CameraManager.Instance.StopMotion();
            else
                CameraManager.Instance.StartMotion();
        };
        Snapper.SnapWhileMoving = false;
        Snapper.SetLogCallback(msg =>
        {
            Debug.LogError(msg);
        });
        Snapper.Init();
    }
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Snapper.TickPerFrame();
    }
}
