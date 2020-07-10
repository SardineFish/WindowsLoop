using System;
using System.Collections.Generic;
using System.Text;

namespace LibTest
{
    public struct Rect
    {
        public Vec2 min, max;
        public Rect(Vec2 min, Vec2 max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
