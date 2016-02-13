using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// A grid of squares, not necessarily with the same width and length.
    /// </summary>
    /// <typeparam name="TileType"></typeparam>
    public class SquareGridPlayArea<TileType> : PlayArea where TileType : MapTile, new()
    {
        public SquareGridPlayArea(int xWidth, int yHeight, float worldUnitsTileWidth, GameObjectManager objMgr)
        {
            m_WorldUnitsTileWidth = worldUnitsTileWidth;
            m_XWidth = xWidth;
            m_YHeight = yHeight;
            Tiles = new TileType[xWidth, yHeight];            
        }

        /// <summary>
        /// The center world coordinate of the origin tile (col0,row0).
        /// </summary>
        public SVector3 OriginTileCenter
        {
            get
            {
                float halfSize = m_WorldUnitsTileWidth / 2;
                return new SVector3(halfSize, halfSize, halfSize);
            }
        }

        public void InitMap()
        {
            SVector3 originTileCenter = OriginTileCenter;

            for (int row = 0; row <= m_XWidth; row++)
            {
                for (int col = 0; col <= m_YHeight; col++)
                {
                    TileType s = new TileType();
                    s.Column = col;
                    s.Row = row;
                    s.WorldLocation = new SVector3();
                    
                    // Set world coordinate
                    s.WorldLocation.X = originTileCenter.X + (s.Column - 1) * m_WorldUnitsTileWidth;
                    s.WorldLocation.Z = originTileCenter.Z + (s.Row - 1) * m_WorldUnitsTileWidth;
                    s.WorldLocation.Y = 0;
        
                    Tiles[row, col] = s;
                }
            }
            
		
        }

        private float m_WorldUnitsTileWidth;
        /// <summary>
        /// Width of a single tile in World units
        /// </summary>
        public float WorldUnitsTileWidth
        {
            get { return m_WorldUnitsTileWidth; }
        }

        private int m_XWidth;
        /// <summary>
        /// Width of map in tiles
        /// </summary>
        public int XWidth
        {
            get { return m_XWidth; }
        }

        private int m_YHeight;
        /// <summary>
        /// Height of map, in tiles
        /// </summary>
        public int YHeight
        {
            get { return m_YHeight; }
            set { m_YHeight = value; }
        }

        private TileType[,] Tiles = null;
        
        /// <summary>
        /// Manhattan distance. Doesn't allow diagonal moves, i.e. they count as two moves (i.e. 1 North then 1 West instead of 1 North-West) 
        /// http://lyfat.wordpress.com/2012/05/22/euclidean-vs-chebyshev-vs-manhattan-distance/
        /// </summary>        
        public static int GetTileDistanceManhattan(int x1, int y1, int x2, int y2)
        {
            int distance = Math.Abs(x2 - x1) + Math.Abs(y2 - y1);
            return distance;
        }

        /// <summary>
        /// Chebyshev  distance. Allows diagonal moves, i.e. they count as one move (i.e. 1 North-West instead of 1 North then 1 West ) 
        /// http://lyfat.wordpress.com/2012/05/22/euclidean-vs-chebyshev-vs-manhattan-distance/
        /// </summary>        
        public static int GetTileDistanceChebyshev(int x1, int y1, int x2, int y2)
        {
            int distance = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
            return distance;
        }

        /// <summary>
        /// Distance between tiles.
        /// http://lyfat.wordpress.com/2012/05/22/euclidean-vs-chebyshev-vs-manhattan-distance/
        /// </summary>
        public static float GetTileDistanceEuclidian(int x1, int y1, int x2, int y2)
        {
            float deltaX = x2 - x1;
            float deltaY = y2 - y1;

            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            return distance;
        }

        public List<MapTile> GetAStarTilePathEuclidian(int x1, int y1, int x2, int y2, IGameObject traveler)
        {
            SpatialAStar<MapTile, IGameObject> aStar = new SpatialAStar<MapTile, IGameObject>(Tiles);
            List<MapTile> path = aStar.Search(new SVector3(x1, y1, 0), new SVector3(x2, y2, 0), traveler);
            return path;
        }

        public List<MapTile> GetAStarTilePathManhattan(int x1, int y1, int x2, int y2, IGameObject traveler)
        {
            SpatialAStarManhattan<MapTile, IGameObject> aStar = new SpatialAStarManhattan<MapTile, IGameObject>(Tiles);
            List<MapTile> path = aStar.Search(new SVector3(x1, y1, 0), new SVector3(x2, y2, 0), traveler);
            return path;
        }

        public List<MapTile> GetAStarTilePathChebyshev(int x1, int y1, int x2, int y2, IGameObject traveler)
        {
            SpatialAStarChebyshev<MapTile, IGameObject> aStar = new SpatialAStarChebyshev<MapTile, IGameObject>(Tiles);
            List<MapTile> path = aStar.Search(new SVector3(x1, y1, 0), new SVector3(x2, y2, 0), traveler);
            return path;
        }

        /// <summary>
        /// Returns the MapTile at the given X/Y coordinates.
        /// </summary>
        /// <param name="x">Row</param>
        /// <param name="y">Column</param>
        /// <returns></returns>
        public TileType this[int x, int y]
        {
            get
            {
                TileType tile = null;
                lock (Tiles)
                {
                    if (Tiles.GetLength(0) > x && Tiles.GetLength(1) > y)
                    {
                        tile = Tiles[x, y];
                    }
                }
                return tile;
            }
            set
            {
                lock (Tiles)
                {
                    if (Tiles.GetLength(0) > x && Tiles.GetLength(1) > y)
                    {
                        if (Tiles[x, y] != null)
                        {
                            this.Regions.Remove(Tiles[x, y]);
                        }
                        this.Regions.Add(value);
                        Tiles[x, y] = value;
                    }
                }
            }
        }

        /// <summary>
        /// Returns all tiles touching the indicated tile. Could return null.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public IEnumerable<TileType> GetAdjacentTiles(int x, int y, int level)
        {
            int[] xOffsets = new int[] { -1 * level, 0, 1 * level, 1 * level, 1 * level, 0, -1, -1 };
            int[] yOffsets = new int[] { -1 * level, -1 * level, -1 * level, 0, 1 * level, 1 * level, 1 * level, 0 };

            for (int i = 0; i < 8; ++i)
            {
                int xOff = x + xOffsets[i];
                int yOff = y + yOffsets[i];
                if (xOff > 0 && yOff > 0)
                {
                    yield return this[xOff, yOff];
                }
            }

            yield break;
        }
     
        /// <summary>
        /// Raycast across the grid and returns every tile that is hit by the cast.
        /// </summary>
        public IEnumerable<TileType> GetTilesInLine(int x0, int y0, int x1, int y1)
        {

            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (steep)
            {
                int t;
                t = x0; // swap x0 and y0
                x0 = y0;
                y0 = t;
                t = x1; // swap x1 and y1
                x1 = y1;
                y1 = t;
            }
            if (x0 > x1)
            {
                int t;
                t = x0; // swap x0 and x1
                x0 = x1;
                x1 = t;
                t = y0; // swap y0 and y1
                y0 = y1;
                y1 = t;
            }
            int dx = x1 - x0;
            int dy = Math.Abs(y1 - y0);
            int error = dx / 2;
            int ystep = (y0 < y1) ? 1 : -1;
            int y = y0;
            for (int x = x0; x <= x1; x++)
            {
                yield return this[(steep ? y : x), (steep ? x : y)];
                error = error - dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }
            yield break;
        }

         /// <summary>
        /// Gets the tile adjacent to the chunk who's coordinates are given in the paramters. 
        /// </summary>
        public TileType GetRelativeTile(int x, int y, Compass.Direction targetRelativeLoc)
        {
            switch (targetRelativeLoc)
            {
                case Compass.Direction.East:
                    x++;
                    break;
                case Compass.Direction.NortEast:
                    x++;
                    y--;
                    break;
                case Compass.Direction.North:
                    y++;
                    break;
                case Compass.Direction.NorthWest:
                    y--;
                    x--;
                    break;
                case Compass.Direction.South:
                    y++;
                    break;
                case Compass.Direction.SouthEast:
                    y++;
                    x++;
                    break;
                case Compass.Direction.SouthWest:
                    x--;
                    y++;
                    break;
                case Compass.Direction.West:
                    x--;
                    break;
            }

            return this[x, y];
        }


    }
}
