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
                        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
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
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Level-0", LoadSceneMode.Single);
                        sceneLoaded = true;
                        PublicData.LevelState = 2;
                        PublicData.Flush();
                    }
                        
                    break;
                }
            case 2:
                {
                    GamePass(3);

                    break;

                }
            case 3:
                {
                    LoadScene("SampleScene", 4);

                    break;
                }

            case 4:
                {
                    if (PublicData.ActiveInstancePID == Snapper.PID)
                    {
                        GamePass(5);
                    }
                    break;
                }
            case 5:
                {
                    LoadScene("Level-2", 6);
                    break;
                }
            case 6:
                {
                    GamePass(7);
                    break;
                }
            case 7:
                {
                    LoadScene("Level-3", 8);
                        break;
                }
            case 8:
                {
                    GamePass(9);
                    break;
                }
            case 9:
                {
                    LoadScene("Staff", 10);
                    break;
                }

        }
    }

    bool GamePass(int nextState)
    {
        if(PublicData.ActiveInstancePID != Snapper.PID)
        {
            return false;
        }
        var pos = GameSystem.Instance.Player.transform.position.ToVector2Int().ToVector3Int();
        var tile = GameMap.Instance.RuntimeMap.GetTile(pos);
        if(tile is GemTile)
        {
            GameMap.Instance.BaseMap.SetTile(pos, null);
            GameMap.Instance.RuntimeMap.SetTile(pos, null);
            AudioManager.Instance.GetGem();


            Debug.LogError("Level complete.");
            PublicData.LevelState = nextState;
            PublicData.Flush();
        }
        return tile is GemTile;
    }

    void LoadScene(string name, int nextState)
    {
        if (!sceneLoaded)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(name, LoadSceneMode.Single);
            sceneLoaded = true;
            PublicData.LevelState = nextState;
            PublicData.Flush();
        }
    }
}
