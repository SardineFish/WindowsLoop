using UnityEngine;
using System.Collections;
using WindowSnap;

public class SnapInit : Singleton<SnapInit>
{
    protected override void Awake()
    {
        Snapper.SnapWhileMoving = false;

        // Setup logger callbacks
        WindowSnap.Logger.LogError = msg => Debug.LogError(msg);
        WindowSnap.Logger.LogWarn = msg => Debug.LogWarning(msg);
        WindowSnap.Logger.LogInfo = msg => Debug.Log(msg);
        WindowSnap.Logger.LogException = ex => Debug.LogException(ex);
        WindowSnap.Logger.Ready();

        WindowSnap.Snapper.SnapWhileMoving = false;


        if (SharedMemory.Others.Count == 0)
        {
            PublicData.GameStage = 0;
            PublicData.ActiveInstancePID = Snapper.PID;
            PublicData.Flush();

        }

        Debug.Log($"PID = {Snapper.PID}");
    }
}
