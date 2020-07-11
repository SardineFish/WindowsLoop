using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindowSnap;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        /*WindowSnap.Snapper.Init();
        WindowSnap.Snapper.SetSnapCallback(OnSnaped);

        WindowSnap.Snapper.SetLogCallback((text) =>
        {
            Debug.LogError(text);
        });*/
    }

    // Update is called once per frame
    void Update()
    {

        //Snapper.SetSnapRect(new WindowSnap.Rect(new Vec2(0, 0), new Vec2(10,10)));
        //Snapper.TickPerSecond();
    }
    void OnSnaped(int pid, Vec2 pos)
    {
        //Debug.Log(pos.ToString());
    }

}
