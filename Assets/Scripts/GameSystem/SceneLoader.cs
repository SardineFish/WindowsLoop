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
            case 2:
                {
                    var pos = GameSystem.Instance.Player.transform.position.ToVector2Int().ToVector3Int();
                    var tile = GameMap.Instance.RuntimeMap.GetTile(pos);
                    if (tile is GemTile)
                    {
                        Debug.LogError("Level complete.");
                        PublicData.LevelState = 3;
                        PublicData.Flush();
                        GameMap.Instance.BaseMap.SetTile(pos, null);
                        GameMap.Instance.RuntimeMap.SetTile(pos, null);
                    }

                    break;

                }

        }
    }
}
