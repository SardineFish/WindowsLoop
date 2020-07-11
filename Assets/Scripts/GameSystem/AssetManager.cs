using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AssetManager : Singleton<AssetManager>
{
    [SerializeField]
    List<UnityEngine.Object> m_Assets;

    public Dictionary<int, UnityEngine.Object> Assets = new Dictionary<int, Object>();

    private void Awake()
    {
        foreach(var asset in m_Assets)
        {
            Assets[asset.GetInstanceID()] = asset;
        }
    }
}
