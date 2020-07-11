using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct StateCache
{
    public float CacheTime { get; set; }

    float lastRenew;

    float updatedTime;

    public bool Value => lastRenew > 0 && updatedTime - lastRenew < CacheTime;


    public StateCache(float cacheTime)
    {
        CacheTime = cacheTime;
        lastRenew = -1;
        updatedTime = 0;
    }

    public void Renew(float time)
    {
        lastRenew = time;
    }
    public void Update(float currentTime)
    {
        updatedTime = currentTime;
    }
    public void Clear()
        => lastRenew = -1;

    public static implicit operator bool(StateCache value)
        => value.Value;

    public override string ToString()
    {
        return Value.ToString();
    }
}