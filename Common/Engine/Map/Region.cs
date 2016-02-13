using System;
using System.Collections.Generic;
using System.Text;
using Shared;

namespace Shared
{
    public class Region : GenericGameObject
    {
        private static uint m_TypeHash = 0;

        public SVector3 WorldLocation { get; set; }

        public override uint TypeHash
        {
            get
            {
                if (m_TypeHash == 0)
                {
                    m_TypeHash = Factory.GetTypeHash(this.GetType());
                }

                return m_TypeHash;
            }
        }


        public Region()
            : base()
        {
            WorldLocation = new SVector3();
        }

        private List<SVector3> m_Geo = new List<SVector3>();
        /// <summary>
        /// The geometry that defines this region
        /// </summary>
        public List<SVector3> Geo
        {
            get 
            { 
                return m_Geo; 
            }
            set
            {
                m_Geo = value;
                RecalculateBounds();
            }
        }

        public void RecalculateBounds()
        {
            if (m_Geo.Count < 2)
            {
                m_xlength = 0;
                m_ylength = 0;
                return;
            }

            m_minx = m_Geo[0].X;
            m_maxx = m_Geo[0].X;
            m_miny = m_Geo[0].Y;
            m_maxy = m_Geo[0].Y;

            foreach (SVector3 pt in m_Geo)
            {
                if (pt.X < m_minx)
                {
                    m_minx = pt.X;
                }

                if (pt.X > m_maxx)
                {
                    m_maxx = pt.X;
                }

                if (pt.Y < m_miny)
                {
                    m_miny = pt.Y;
                }

                if (pt.Y > m_maxy)
                {
                    m_maxy = pt.Y;
                }
            }

            m_xlength = Math.Abs(m_maxx - m_minx);
            m_ylength = Math.Abs(m_maxy - m_miny);
        }

     

        private string m_Description;
        /// <summary>
        /// Seen in the UI and displayed to the player, if necessary
        /// </summary>
        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        private string m_ContentTag;
        /// <summary>
        /// something that identifies the content associated with this region
        /// </summary>
        public string ContentTag
        {
            get { return m_ContentTag; }
            set { m_ContentTag = value; }
        }

        private string m_InternalName = "";
        /// <summary>
        /// The name as known to the game
        /// </summary>
        public string InternalName
        {
            get { return m_InternalName; }
            set { m_InternalName = value; }
        }

        private double m_minx = 0;
        private double m_miny = 0;
        private double m_maxx = 0;
        private double m_maxy = 0;
        private double m_xlength = 0;
        private double m_ylength = 0;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="pts">The array of Vector used to create the PolygonF.</param>
        public Region(List<SVector3> pts)
        {
            Geo = pts;
        }

        /// <summary>
        /// The Rectangular, axis aligned (unrotated), Bounds of the Polygon.
        /// </summary>
        public Rectangle AABounds
        {
            get
            {
                return new Rectangle(m_minx, m_miny, m_maxx - m_minx, m_maxy - m_miny);
            }
        }

        /// <summary>
        /// The Minimum X coordinate value in the Vector collection.
        /// </summary>
        public double MinimumX
        {
            get { return m_minx; }
        }

        /// <summary>
        /// The Maximum X coordinate value in the Vector collection.
        /// </summary>
        public double MaximumX
        {
            get { return m_maxx; }
        }

        /// <summary>
        /// The Minimum Y coordinate value in the Vector collection.
        /// </summary>
        public double MinimumY
        {
            get { return m_miny; }
        }

        /// <summary>
        /// The Maximum Y coordinate value in the Vector collection.
        /// </summary>
        public double MaximumY
        {
            get { return m_maxy; }
        }

        /// <summary>
        /// The number of Points in the Polygon.
        /// </summary>
        public int NumberOfPoints
        {
            get { return m_Geo.Count; }
        }

        /// <summary>
        /// Compares the supplied point and determines whether or not it is inside the Rectangular Bounds
        /// of the Polygon.
        /// </summary>
        /// <param name="pt">The Vector to compare.</param>
        /// <returns>True if the Vector is within the Rectangular Bounds, False if it is not.</returns>
        public bool IsInBounds(SVector3 pt)
        {
            return AABounds.Contains(pt);
        }

        /// <summary>
        /// Compares the supplied point and determines whether or not it is inside the Actual Bounds
        /// of the Polygon.
        /// </summary>
        /// <remarks>The calculation formula was converted from the C version available at
        /// http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
        /// </remarks>
        /// <param name="pt">The Vector to compare.</param>
        /// <returns>True if the Vector is within the Actual Bounds, False if it is not.</returns>
        public bool Contains(SVector3 pt)
        {
            bool isIn = false;

            if (IsInBounds(pt))
            {
                int i, j = 0;

                // The following code is converted from a C version found at 
                // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
                for (i = 0, j = NumberOfPoints - 1; i < NumberOfPoints; j = i++)
                {
                    if (
                        (
                         ((m_Geo[i].Y <= pt.Y) && (pt.Y < m_Geo[j].Y)) || ((m_Geo[j].Y <= pt.Y) && (pt.Y < m_Geo[i].Y))
                        ) &&
                        (pt.X < (m_Geo[j].X - m_Geo[i].X) * (pt.Y - m_Geo[i].Y) / (m_Geo[j].Y - m_Geo[i].Y) + m_Geo[i].X)
                       )
                    {
                        isIn = !isIn;
                    }
                }
            }

            return isIn;
        }

        /// <summary>
        /// Returns the Vector that represents the center of the Rectangular Bounds of the Polygon.
        /// </summary>
        public SVector3 CenterPointOfBounds
        {
            get
            {
                double x = m_minx + (m_xlength / 2);
                double y = m_miny + (m_ylength / 2);
                return new SVector3(x, y, 0);
            }
        }

        /// <summary>
        /// NOT YET IMPLEMENTED.  Currently returns the same as CenterPointOfBounds.
        /// This is intended to be the Visual Center of the Polygon, and will be implemented
        /// once I can figure out how to calculate that Point.
        /// </summary>
        public SVector3 CenterPoint
        {
            get
            {
                SVector3 pt = CenterPointOfBounds;
                return pt;
            }
        }

        /// <summary>
        /// Calculates the Area of the Polygon.
        /// </summary>
        public decimal Area
        {
            get
            {
                decimal xy = 0M;
                for (int i = 0; i < m_Geo.Count; i++)
                {
                    SVector3 pt1;
                    SVector3 pt2;
                    if (i == m_Geo.Count - 1)
                    {
                        pt1 = m_Geo[i];
                        pt2 = m_Geo[0];
                    }
                    else
                    {
                        pt1 = m_Geo[i];
                        pt2 = m_Geo[i + 1];
                    }
                    xy += Convert.ToDecimal(pt1.X * pt2.Y);
                    xy -= Convert.ToDecimal(pt1.Y * pt2.X);
                }

                decimal area = Convert.ToDecimal(Math.Abs(xy)) * .5M;

                return area;
            }
        }


       
    }
}
