using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public class Rectangle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public Rectangle(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Contains(SVector3 pt)
        {
            return
                pt.X > X && pt.X < X + Width &&
                pt.Y > Y && pt.Y < Y + Height;
        }

    }
}
