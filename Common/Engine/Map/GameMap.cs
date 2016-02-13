using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Shared
{

    public class GameMap
    {
        //public static string MAP_DIR = System.Environment.CurrentDirectory + "\\Maps\\";
        
        private GameMap() { }

        protected void ReadObjectDataFromTable(Dictionary<string, byte[]> data)
        {
            if (data.ContainsKey("dimX"))
            {
                m_DimensionX = BitConverter.ToDouble((byte[])data["dimX"], 0);
            }

            if (data.ContainsKey("dimY"))
            {
                m_DimensionY = BitConverter.ToDouble((byte[])data["dimY"], 0);
            }

            if (data.ContainsKey("dimZ"))
            {
                m_DimensionZ = BitConverter.ToDouble((byte[])data["dimZ"], 0);
            }

            if (data.ContainsKey("chSizeX"))
            {
                m_ChunkSizeX = BitConverter.ToDouble((byte[])data["chSizeX"], 0);
            }

            if (data.ContainsKey("chSizeY"))
            {
                m_ChunkSizeY = BitConverter.ToDouble((byte[])data["chSizeY"], 0);
            }

            if (data.ContainsKey("chSizeZ"))
            {
                m_ChunkSizeZ = BitConverter.ToDouble((byte[])data["chSizeZ"], 0);
            }

            if (data.ContainsKey("scale"))
            {
                m_Scale = BitConverter.ToDouble((byte[])data["scale"], 0);
            }

            if (data.ContainsKey("mapName"))
            {
                byte[] dat = (byte[])data["mapName"];
                m_Name = Encoding.UTF8.GetString(dat, 0, dat.Length);
            }

            if (data.ContainsKey("bg"))
            {

                byte[] dat = (byte[])data["bg"];
                m_MapBackground = Encoding.UTF8.GetString(dat, 0, dat.Length);
            }


            // load region data
            if (data.ContainsKey("regions"))
            {
                System.IO.MemoryStream memStream = new System.IO.MemoryStream((byte[])data["regions"]);
                System.IO.BinaryReader read = new System.IO.BinaryReader(memStream);
                int numRegions = read.ReadInt32(); // number of regions
                for (int i = 0; i < numRegions; i++)
                {
                    Region r = new Region();

                    // read region name
                    int nameLen = read.ReadInt32(); // read name length
                    byte[] datName = read.ReadBytes(nameLen);
                    r.ObjectName = Encoding.UTF8.GetString(datName, 0, datName.Length);

                    // read region internal name
                    int iNameLen = read.ReadInt32();
                    byte[] datIName = read.ReadBytes(iNameLen);
                    r.InternalName = Encoding.UTF8.GetString(datIName, 0, datIName.Length);

                    // read location
                    r.WorldLocation = new SVector3(read.ReadDouble(), read.ReadDouble(), read.ReadDouble());

                    // Read path geo 
                    int numPoints = read.ReadInt32();
                    List<SVector3> pGeo = new List<SVector3>();
                    for (int v = 0; v < numPoints; v++)
                    {
                        SVector3 vPoint = new SVector3(read.ReadDouble(), read.ReadDouble(), read.ReadDouble());
                        pGeo.Add(vPoint);
                    }

                    r.Geo = pGeo;
                    
                    // region ID
                    int UIDLen = read.ReadInt32(); // read title length
                    byte[] datUid = read.ReadBytes(UIDLen);
                    string uid = Encoding.UTF8.GetString(datUid, 0, datUid.Length);

                    r.UID = new Guid(uid);

                    // title
                    int titleLen = read.ReadInt32(); // read title length
                    byte[] datTitle = read.ReadBytes(titleLen);
                    r.ObjectName = Encoding.UTF8.GetString(datTitle, 0, datTitle.Length);

                    // description
                    int descLen = read.ReadInt32(); // read desc length
                    byte[] datDesc = read.ReadBytes(descLen);
                    r.Description = Encoding.UTF8.GetString(datDesc, 0, datDesc.Length);

                    // content id
                    int cIDLen = read.ReadInt32(); // content id length
                    byte[] datCont = read.ReadBytes(cIDLen);
                    r.ContentTag = Encoding.UTF8.GetString(datCont, 0, datCont.Length);

                    m_Regions.AddObject(r);
                }
            }         
        }

        public Region GetRegionAtLoc(SVector3 v)
        {
            Region r = null;
            Dictionary<Guid, IGameObject>.ValueCollection.Enumerator enu = m_Regions.Objects;
            while(enu.MoveNext())
            {
                Region reg = enu.Current as Region;
                if (!reg.IsInBounds(v))
                {
                    continue;
                }

                if (reg.Contains(v))
                {
                    return reg;
                }
            }

            return r;
        }

        protected void WriteObjectDataToTable(Dictionary<string, byte[]> data)
        {
            data.Add("dimX", BitConverter.GetBytes(m_DimensionX));
            data.Add("dimY", BitConverter.GetBytes(m_DimensionY));
            data.Add("dimZ", BitConverter.GetBytes(m_DimensionZ));
            data.Add("chSizeX", BitConverter.GetBytes(m_ChunkSizeX));
            data.Add("chSizeY", BitConverter.GetBytes(m_ChunkSizeY));
            data.Add("chSizeZ", BitConverter.GetBytes(m_ChunkSizeZ));
            data.Add("scale", BitConverter.GetBytes(m_Scale));
            data.Add("mapName", System.Text.Encoding.UTF8.GetBytes(m_Name));
            data.Add("bg", System.Text.Encoding.UTF8.GetBytes(m_MapBackground));
            // write region data
            List<IGameObject> regions = m_Regions.GetObjectsOfType(GOT.Region);
            List<byte> dat = new List<byte>();
            dat.AddRange(BitConverter.GetBytes(regions.Count)); // number of regions
            for (int i = 0; i < regions.Count; i++)
            {
                Region r = (Region)regions[i];

                // Write region name
                byte[] regionName = System.Text.Encoding.UTF8.GetBytes(r.ObjectName); // convert to bytes
                dat.AddRange(BitConverter.GetBytes(regionName.Length)); // write name length
                dat.AddRange(regionName); // write name

                // Write region internal name
                byte[] regionIName = System.Text.Encoding.UTF8.GetBytes(r.InternalName); // convert to bytes
                dat.AddRange(BitConverter.GetBytes(regionIName.Length)); // write name length
                dat.AddRange(regionIName); // write name

                // Write location
                dat.AddRange(BitConverter.GetBytes(r.WorldLocation.X));
                dat.AddRange(BitConverter.GetBytes(r.WorldLocation.Y));
                dat.AddRange(BitConverter.GetBytes(r.WorldLocation.Z));

                // Write region geo
                dat.AddRange(BitConverter.GetBytes(r.Geo.Count));
                foreach (SVector3 v in r.Geo)
                {
                    dat.AddRange(BitConverter.GetBytes(v.X));
                    dat.AddRange(BitConverter.GetBytes(v.Y));
                    dat.AddRange(BitConverter.GetBytes(v.Z));
                }

                /*
                byte[] pathGeo = System.Text.Encoding.UTF8.GetBytes(r.Geo.ToString()); // convert geo to string and then to bytes
                dat.AddRange(BitConverter.GetBytes(pathGeo.Length));// write geo string length
                dat.AddRange(pathGeo); // write geo string (in byte format)
                */

                // region ID
                byte[] uidBytes = System.Text.Encoding.UTF8.GetBytes(r.UID.ToString()); // get bytes
                dat.AddRange(BitConverter.GetBytes(uidBytes.Length)); // write length
                dat.AddRange(uidBytes); // write id
                
                // region title
                byte[] regionTitle = System.Text.Encoding.UTF8.GetBytes(r.ObjectName); // convert to bytes
                dat.AddRange(BitConverter.GetBytes(regionTitle.Length)); // write title length
                dat.AddRange(regionTitle); // write title
   
                // region description
                byte[] regionDesc = System.Text.Encoding.UTF8.GetBytes(r.Description); // convert to bytes
                dat.AddRange(BitConverter.GetBytes(regionDesc.Length)); // write Description length
                dat.AddRange(regionDesc); // write Description

                // content ID
                byte[] cID = System.Text.Encoding.UTF8.GetBytes(r.ContentTag); // convert to bytes
                dat.AddRange(BitConverter.GetBytes(cID.Length)); // write title length
                dat.AddRange(cID); // write title   
            }

            data.Add("regions", dat.ToArray());
        }

        /// <summary>
        /// Creates the meweap object
        /// </summary>
        /// <param name="mapName"></param>
        /// <param name="mapPath"></param>
        /// <param name="dimensionX"></param>
        /// <param name="dimensionY"></param>
        /// <param name="dimensionZ"></param>
        /// <param name="chunkSizeX"></param>
        /// <param name="chunkSizeY"></param>
        /// <param name="chunkSizeZ"></param>
        /// <param name="bg"></param>
        /// <returns></returns>
        public static GameMap CreateMap(string mapName, double dimensionX, double dimensionY, double dimensionZ, double chunkSizeX, double chunkSizeY, double chunkSizeZ)
        {
            GameMap m = new GameMap(dimensionX, dimensionY, dimensionZ, chunkSizeX, chunkSizeY, chunkSizeZ);
            m.m_Name = mapName;
            return m;
        }

        private GameMap(double dimensionX, double dimensionY, double dimensionZ, double chunkSizeX, double chunkSizeY, double chunkSizeZ) 
        {
            m_MapID = Guid.NewGuid(); 
            m_DimensionX = dimensionX ;
            m_DimensionY = dimensionY;
            m_DimensionZ = dimensionZ;

            // Chunk sizes.  Can't be zero. if chunk size is given as zero, we dump everything into one big chunk the size of the map
            m_ChunkSizeX = (chunkSizeX > 0 && chunkSizeX < dimensionX) ? chunkSizeX : dimensionX;
            m_ChunkSizeY = (chunkSizeY > 0 && chunkSizeY < dimensionY) ? chunkSizeY : dimensionY;
            m_ChunkSizeZ = (chunkSizeZ > 0 && chunkSizeZ < dimensionZ) ? chunkSizeZ : dimensionZ; 

            // create mapchunk array
            int xChunks = (int) Math.Ceiling(dimensionX / m_ChunkSizeX);
            int yChunks = (int)Math.Ceiling(dimensionY / m_ChunkSizeY);
            int zChunks = (int)Math.Ceiling(dimensionZ / m_ChunkSizeZ);
            m_Chunks = new MapChunk[xChunks, yChunks, zChunks];            
        }

        protected Guid m_MapID = Guid.Empty;
        /// <summary>
        /// Unique ID for the map
        /// </summary>
        public Guid MapID
        {
            get { return m_MapID; }
        }
        
        private double m_Scale = 5;
        /// <summary>
        /// Meters per pixel
        /// </summary>
        public double Scale
        {
            get { return m_Scale; }
            set { m_Scale = value; }
        }

        private string m_Name = "default_map";
        /// <summary>
        /// The map's name. 
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        private string m_Description = "";
        /// <summary>
        /// Player visible description
        /// </summary>
        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }
        

        private double m_DimensionZ = 0;
        /// <summary>
        /// How "high" the map is, in meters
        /// </summary>
        public double DimensionZ
        {
            get { return m_DimensionZ; }
        }
	

        private double m_DimensionX = 1f;

        /// <summary>
        /// How "wide" the map is, in meters
        /// </summary>
        public double DimensionX
        {
            get { return m_DimensionX; }
        }

        private double m_DimensionY = 1f;

        private string m_MapBackground;
        /// <summary>
        /// Identifies the background image of the map;
        /// </summary>
        public string MapBackground
        {
            get { return m_MapBackground; }
            set { m_MapBackground = value; }
        }
        

        /// <summary>
        /// How "tall" the map is, in meters
        /// </summary>
        public double DimensionY
        {
            get { return m_DimensionY; }
        }

        private double m_ChunkSizeX;
        /// <summary>
        /// The size of a chunk, in map units
        /// </summary>
        public double ChunkSizeX
        {
            get { return m_ChunkSizeX; }
        }

        private double m_ChunkSizeY;
        /// <summary>
        /// The size of a chunk, in map units
        /// </summary>
        public double ChunkSizeY
        {
            get { return m_ChunkSizeY; }
        }

        private double m_ChunkSizeZ;
        /// <summary>
        /// The size of a chunk, in map units
        /// </summary>
        public double ChunkSizeZ
        {
            get { return m_ChunkSizeZ; }
        }

        /// <summary>
        /// Gets the chunk adjacent to the chunk who's coordinates are given in the paramters. 
        /// </summary>
        /// <param name="curChunkX">coord in the chunk grid</param>
        /// <param name="curChunkY">coord in the chunk grid</param>
        /// <param name="curChunkZ">coord in the chunk grid</param>
        /// <param name="targetRelativeLoc">determines which adjacent chunk is returned</param>
        /// <returns></returns>
        public MapChunk GetRelativeChunk(int curChunkCoordX, int curChunkCoordY, int curChunkCoordZ, Compass.Direction targetRelativeLoc)
        {
            switch(targetRelativeLoc)
            {
                case Compass.Direction.East:
                    curChunkCoordX++;
                    break;
                case Compass.Direction.NortEast:
                    curChunkCoordX++;
                    curChunkCoordY--;
                    break;
                case Compass.Direction.North:
                    curChunkCoordY++;
                    break;
                case Compass.Direction.NorthWest:
                    curChunkCoordY--;
                    curChunkCoordX--;
                    break;
                case Compass.Direction.South:
                    curChunkCoordY++;
                    break;
                case Compass.Direction.SouthEast:
                    curChunkCoordY++;
                    curChunkCoordX++;
                    break;
                case Compass.Direction.SouthWest:
                    curChunkCoordX--;
                    curChunkCoordY++;
                    break;
                case Compass.Direction.West:
                    curChunkCoordX--;
                    break;
                case Compass.Direction.Down:
                    curChunkCoordZ--;
                    break;
                case Compass.Direction.Up:
                    curChunkCoordZ++;
                    break;
            }
            
            return GetChunkAtChunkCoord(new SVector3(curChunkCoordX, curChunkCoordY, curChunkCoordZ));
        }

        /// <summary>
        /// Returns a chunk's coordinate address within the chunk grid, given a location on the world map
        /// </summary>
        /// <param name="mapLoc"></param>
        /// <returns></returns>
        public SVector3 GetChunkCoordFromMapLoc(SVector3 mapLoc)
        {
            if (!IsLocationWithinMap(mapLoc))
            {
                return null;
            }

            double chunkX = (double)Math.Floor(mapLoc.X / m_ChunkSizeX);
            double chunkY = (double)Math.Floor(mapLoc.Y / m_ChunkSizeY);
            double chunkZ = (double)Math.Floor(mapLoc.Z / m_ChunkSizeZ);

            return new SVector3(chunkX, chunkY, chunkZ);
        }

        /// <summary>
        /// Returns a map chunk (loads it from datastore if not currently in memory), given it's coordinates within the chunk grid
        /// </summary>
        /// <param name="chunkCoord"></param>
        /// <returns></returns>
        public MapChunk GetChunkAtChunkCoord(SVector3 chunkCoord)
        {
            if (chunkCoord.X > m_Chunks.GetUpperBound(0) || chunkCoord.Y > m_Chunks.GetUpperBound(1) || chunkCoord.Z > m_Chunks.GetUpperBound(2))
            {
                return null;
            }

            MapChunk chunk = m_Chunks[(int)chunkCoord.X, (int)chunkCoord.Y, (int)chunkCoord.Z];
            return chunk;
        }

        /// <summary>
        /// Returns a reference to a map chunk (and all contained GameObjects), given a location
        /// </summary>
        /// <param name="loc"></param>
        /// <returns></returns>
        public MapChunk GetChunkAtMapLoc(SVector3 mapLoc)
        {
            SVector3 chunkCoord = GetChunkCoordFromMapLoc(mapLoc);
            if (chunkCoord == null)
            {
                return null;
            }

            MapChunk chunk = GetChunkAtChunkCoord(chunkCoord);
            if (chunk == null)
            {
                throw new Exception("Error loading map chunk.");
            }

            return chunk;
        }

        /// <summary>
        /// All of the map chunks in the map.  If a given coordinate is null, then the chunk isn't currently loaded into memory
        /// and must be loaded (either from the disk or the DB, depending on where it's stored)
        /// </summary>
        private MapChunk[,,] m_Chunks = null;

        /// <summary>
        /// Determines if a given location is valid, given the size of the map.
        /// </summary>
        /// <param name="loc"></param>
        /// <returns></returns>
        public bool IsLocationWithinMap(SVector3 loc)
        {
            return (loc.X <= m_DimensionX && loc.Y <= m_DimensionY && loc.Z <= m_DimensionZ);
        }

        private GameObjectContainer m_Inventory = new GameObjectContainer();

        /// <summary>
        /// Stores all global map objects.  These are the objects that are stored independantly of the chunks
        /// and are loaded into memory as long as the map is loaded.  This is used for any object that all players must know about at all times, 
        /// regardless of whether or not the mapchunk within which the object is located is currently loaded in memory
        /// </summary>
        public GameObjectContainer Inventory
        {
            get { return m_Inventory; }
        }

        private GameObjectContainer m_Regions = new GameObjectContainer();

        /// <summary>
        /// Stores all global STATIC map objects.  These are the objects that are stored independantly of the chunks
        /// and are loaded into memory as long as any instance of the map is loaded.  This data is the same for all instances of this map
        /// This is where regions are stored, for example.
        /// </summary>
        public GameObjectContainer Regions
        {
            get { return m_Regions; }
        }

        public static GameMap LoadFromDisk(string p)
        {
            GameMap map = new GameMap();
            Dictionary<string, byte[]> th =  PersistableDiskObject.LoadFromDisk(p);
            if (th == null)
            {
                return null;
            }
            map.ReadObjectDataFromTable(th);
            return map;
        }

        public void SaveToDisk(string file)
        {
            Dictionary<string, byte[]> data = new Dictionary<string, byte[]>();            
            WriteObjectDataToTable(data);
            PersistableDiskObject.SaveToDisk(data, file);            
        }


    }
}
