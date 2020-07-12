using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Singleton<T> : UnityEngine.MonoBehaviour where T:Singleton<T>
{
    private static T instance = null;
    public static T Instance
    {
        get
        {
            return instance;
            if (instance)
                return instance;

            var obj = new UnityEngine.GameObject();
            var component = obj.AddComponent<T>();
            DontDestroyOnLoad(obj);
            instance = component;

            return component;
        }
    }

    public Singleton()
    {
        instance = this as T;
    }

    protected virtual void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (instance && instance != this)
        {
            Destroy(gameObject);
        }
        else if(!instance)
        {
            instance = this as T;
        }

    }
}