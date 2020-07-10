using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibTest;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        LibTest.Foo.SetSnapCallback(OnSnaped);
        LibTest.Foo.SetLogCallback((text) =>
        {
            Debug.LogError(text);
        });
        StartCoroutine(MoveWindow());
    }

    // Update is called once per frame
    void Update()
    {
        LibTest.Foo.SetSnapRect(new LibTest.Rect(new Vec2(0, 0), new Vec2(10,10)));
    }
    IEnumerator MoveWindow()
    {
        while(true)
        {
            Foo.TickPerSecond();
            yield return new WaitForSeconds(1);
        }
    }
    void OnSnaped(int pid, LibTest.Vec2 pos)
    {
        //Debug.Log(pos.ToString());
    }

}
