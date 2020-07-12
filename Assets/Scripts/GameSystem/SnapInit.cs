using UnityEngine;
using System.Collections;
using WindowSnap;

public class SnapInit : Singleton<SnapInit>
{
    protected override void Awake()
    {
        Snapper.SnapWhileMoving = false;
        Snapper.SetLogCallback(msg =>
        {
            Debug.LogError(msg);
        });
        Snapper.Init();


        if (SharedMemory.Others.Count == 0)
        {
            PublicData.LevelState = 0;
            PublicData.ActiveInstancePID = Snapper.PID;
            PublicData.Flush();
        }



        if (SharedMemory.Others.Count == 0 && !Application.isEditor)
        {

            var path = System.Environment.GetCommandLineArgs()[0];
            System.Diagnostics.Process.Start(path, "-batchmode -nographics -audiohost");

            //System.Diagnostics.Process.Start(System.Environment.);
        }
    }
}
