using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AssetManager : Singleton<AssetManager>
{
    [SerializeField]
    List<UnityEngine.Object> m_Assets;

    public Dictionary<int, UnityEngine.Object> Assets = new Dictionary<int, Object>();
    public Dictionary<UnityEngine.Object, int> IDMap = new Dictionary<Object, int>();

    bool init = false;

    protected override void Awake()
    {
        base.Awake();

        if (init)
            return; 

        for (int i = 0; i < m_Assets.Count; i++)
        {
            Assets[i] = m_Assets[i];
            IDMap[m_Assets[i]] = i;
        }
        foreach(var asset in m_Assets)
        {
            Assets[asset.GetInstanceID()] = asset;
        }

        init = true;
    }

    public int GetID(UnityEngine.Object asset)
    {
        if (!init)
            Awake();
        if (IDMap.ContainsKey(asset))
            return IDMap[asset];
        return -1;
    }
}
