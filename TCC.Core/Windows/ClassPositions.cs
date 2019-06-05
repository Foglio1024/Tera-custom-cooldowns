﻿using System;
using System.Collections.Generic;
using System.Windows;
using TCC.Data;
using TeraDataLite;

namespace TCC.Windows
{
    public class ClassPositionsData
    {
        public Point Position { get; set; }
        public ButtonsPosition Buttons { get; set; }

        public ClassPositionsData()
        {
                
        }
        public ClassPositionsData(ClassPositionsData origin)
        {
            Position = new Point(origin.Position.X, origin.Position.Y);
            Buttons = origin.Buttons;
        }

        public ClassPositionsData(double x, double y, ButtonsPosition buttons)
        {
            Position = new Point(x, y);
            Buttons = buttons;
        }

        public void ApplyCorrection(Size sc)
        {
            Position = new Point(sc.Width * Position.X, sc.Height * Position.Y);
        }
    }

    public class ClassPositions
    {
        public Dictionary<Class, ClassPositionsData> Classes { get; set; }

        public ClassPositions()
        {
            Classes = new Dictionary<Class, ClassPositionsData>();
            foreach (Class cl in Enum.GetValues(typeof(Class)))
            {
                Classes.Add(cl, new ClassPositionsData(0, 0, ButtonsPosition.Above));
            }
        }

        public ClassPositions(ClassPositions origin)
        {
            Classes = new Dictionary<Class, ClassPositionsData>();
            foreach (Class cl in Enum.GetValues(typeof(Class)))
            {
                Classes.Add(cl, new ClassPositionsData(origin.Classes[cl]));
            }
        }

        public ClassPositions(double x, double y, ButtonsPosition buttons)
        {
            Classes = new Dictionary<Class, ClassPositionsData>();
            foreach (Class cl in Enum.GetValues(typeof(Class)))
            {
                Classes.Add(cl, new ClassPositionsData(x, y, buttons));
            }
        }

        public void SetPosition(Class cname, Point position)
        {
            Classes[cname].Position = position;
        }

        public void SetAllPositions(Point position)
        {
            foreach (Class cl in Enum.GetValues(typeof(Class)))
            {
                Classes[cl].Position = position;
            }
        }

        public void ApplyCorrection(Size sc)
        {
            foreach (Class cl in Enum.GetValues(typeof(Class)))
            {
                Classes[cl].ApplyCorrection(sc);
            }
        }
        public void SetButtons(Class cname, ButtonsPosition buttons)
        {
            Classes[cname].Buttons = buttons;
        }

        public Point Position(Class cname)
        {
            return Classes[cname].Position;
        }

        public ButtonsPosition Buttons(Class cname)
        {
            return Classes[cname].Buttons;
        }
    }
}
