using UnityEngine;
using System.Collections;
using WindowSnap;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class SceneLoader : Singleton<SceneLoader>
{
    [System.Serializable]
    public struct GameStage
    {
        [SerializeField]
        public List<string> Scenes;
    }

    public bool sceneLoaded = false;
    public List<GameStage> GameStages = new List<GameStage>();
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

        var currentStage = PublicData.GameStage;

        if(!sceneLoaded)
        {
            var others = SharedMemory.Others.Select(page => page.ReadInt32(GameDataAddr.PID)).ToList();
            var stageData = GameStages[currentStage];
            for (int i = 0; i < stageData.Scenes.Count; i++)
            {
                var scenePID = PublicData.GetScenePID(i);
                if(!others.Any(pid=>pid == scenePID))
                {
                    SceneManager.LoadScene(stageData.Scenes[i], LoadSceneMode.Single);
                    PublicData.SetScenePID(i, Snapper.PID);
                    PublicData.Flush();
                    break;
                }
            }
            sceneLoaded = true;
        }
        else if(PublicData.ActiveInstancePID == Snapper.PID)
        {
            var pos = GameSystem.Instance.Player.transform.position.ToVector2Int().ToVector3Int();
            var tile = GameMap.Instance.RuntimeMap.GetTile(pos);
            if (tile is GemTile)
            {
                GameMap.Instance.SetBaseTileAt(pos.x, pos.y, null);
                GameMap.Instance.RuntimeMap.SetTile(pos, null);
                AudioManager.Instance.GetGem();

                Debug.LogError("Level complete.");
                PublicData.GameStage++;
                PublicData.Flush();
            }
        }


    }
}
