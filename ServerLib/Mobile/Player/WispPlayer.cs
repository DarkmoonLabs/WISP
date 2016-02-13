using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class WispPlayer : ServerGameObject, IMobile
    {
        public WispPlayer(ServerCharacterInfo ci) : base()
        {
            Initialize(ci);
            Version = 0; // Update the version of the serialize or deserialize methods have been updated. that way the correct datastream is read from the database.
        }

        public void Initialize(ServerCharacterInfo ci)
        {
            CharacterData = new ServerCharacterInfo(ci.CharacterInfo, ci.OwningAccount);
        }

        public WispPlayer()            : base()
        {            
        
        }

        private MobileState m_PhysicalState = new MobileState();
        new public MobileState PhysicalState
        {
            get
            {
                return m_PhysicalState;
            }
            set
            {
                m_PhysicalState = value;
            }
        }


        public ServerCharacterInfo CharacterData { get; set; }

        private List<MobileState> m_StateHistory = new List<MobileState>();
        public List<MobileState> StateHistory
        {
            get
            {
                return m_StateHistory;
            }
            set
            {
                m_StateHistory = value;
            }
        }

        private void SerializeVersion0(ref byte[] buffer, Pointer p)
        {
        }

        private void DeserializeVersion0(byte[] data, Pointer p)
        {
        }

        public override void Serialize(ref byte[] buffer, Pointer p)
        {
            base.Serialize(ref buffer, p);
            switch(Version)
            {
                case 0:
                    SerializeVersion0(ref buffer, p);
                    break;
            }

            // Load the character data, since WispPlayer is a composite object and we want to save the 
            string msg = "";
            CharacterUtil.Instance.SaveCharacter(CharacterData.OwningAccount, CharacterData, false, ref msg);
        }

        public override void Deserialize(byte[] data, Pointer p)
        {
            base.Deserialize(data, p);
            switch(Version)
            {
                case 0:
                    DeserializeVersion0(data, p);
                    break;
            }
            // No need to load the character data, since the character data is always loaded and sent along the wire.
        }
        
   
    }
}
