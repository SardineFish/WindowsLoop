namespace WindowSnap
{
    public struct Rect
    {
        public Vec2 Min, Max;
        public Rect(Vec2 min, Vec2 max)
        {
            Min = min;
            Max = max;
        }
    }
}
