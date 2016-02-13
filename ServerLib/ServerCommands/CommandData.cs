using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Encapsulates one command that is sent from the Zeus Client to the server
    /// </summary>
    public class CommandData : ISerializableWispObject, IComparable
    {
        public CommandData()
        {
            AllowedRoles = new string[0];
        }

        /// <summary>
        /// Unique ID of the command
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// All of the roles that can execute this command
        /// </summary>
        public string[] AllowedRoles { get; set; }

        /// <summary>
        /// Which assembly does this command exist in
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Which class does the command exist in
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// The actual method to invoke for the command
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Parameter names.  Also defines the number of expected parameters. Optional parameters are not supported.
        /// </summary>
        public string[] ParmNames { get; set; }

        /// <summary>
        /// Any string that would help someone execute the command
        /// </summary>
        public string UsageHelp { get; set; }

        /// <summary>
        /// The group of commands that this command belongs to, i.e "Character", "Server", "User", etc. Can be any string.
        /// </summary>
        public string CommandGroup { get; set; }

        private static uint m_TypeHash = 0;
        public uint TypeHash
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

        public void Serialize(ref byte[] buffer, Pointer p)
        {
            BitPacker.AddString(ref buffer, p, CommandName);
            BitPacker.AddString(ref buffer, p, CommandGroup);
            BitPacker.AddString(ref buffer, p, UsageHelp);
            BitPacker.AddStringList(ref buffer, p, new List<string>(AllowedRoles));
            BitPacker.AddStringList(ref buffer, p, new List<string>(ParmNames));
        }

        public void Deserialize(byte[] data, Pointer p)
        {
            CommandName = BitPacker.GetString(data, p);
            CommandGroup = BitPacker.GetString(data, p);
            UsageHelp = BitPacker.GetString(data, p);
            AllowedRoles = BitPacker.GetStringList(data, p).ToArray();
            ParmNames = BitPacker.GetStringList(data, p).ToArray();
        }

        public int CompareTo(object obj)
        {
            return this.CommandName.CompareTo(((CommandData)obj).CommandName);
        }
    }
}
