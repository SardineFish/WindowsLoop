using UnityEngine;
using System.Collections;
using WindowSnap;
using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    public bool sceneLoaded = false;
    protected override void Awake()
    {
        base.Awake();
    }
    void Update()
    {
        if (AudioManager.Instance.IsAudioHost)
            return;
        if(PublicData.ActiveInstancePID == Snapper.PID)
        {
        }

        switch(PublicData.LevelState)
        {
            case 0:
                {
                    if(!sceneLoaded)
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Level-0", LoadSceneMode.Single);
                        sceneLoaded = true;
                        PublicData.LevelState = 1;
                        PublicData.Flush();
                    }
                    break;
                }
            case 1:
                {
                    if(!sceneLoaded)
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Level-0-Assist", LoadSceneMode.Single);
                        sceneLoaded = true;
                        PublicData.LevelState = 2;
                        PublicData.Flush();
                    }
                    break;
                }

        }
    }
}
