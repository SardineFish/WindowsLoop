using System;
using System.Collections.Generic;
using System.Text;

namespace LibTest
{
    public struct Vec2
    {
        public float x, y;
        public Vec2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return $"({this.x}, {this.y})";
        }
    }
}
