
using Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

public delegate void ClusterServerDisconnectedDelegate(IClientConnection connection, Client sender, string msg);

public enum ClientConnectionPhase : int
{
    Unconnected = 0,
    LoginServerInitiatedConnection = 1,
    LoginServerConnected = 2,
    LoginServerGreeting = 3,
    LoginServerConnectionEncrypted = 4,
    LoginServerLoggedIn = 5,
    LoginServerGotWorldServerListing = 6,
    LoginServerRequestedClusterServerAccess = 7,
    LoginServerGotClusterServerAccess = 8,
    CentralServerInitiatedConnection = 9,
    CentralServerConnected = 10,
    CentralServerGreeting = 11,    
    CentralServerReadyForCommunication = 12,
    CentralServerRequestAccessContent = 16,
    WorldServerInitiatedConnection = 17,
    WorldServerConnected = 18,
    WorldServerGreeting = 19,    
    WorldServerReadyForPlay = 20,
    WorldServerAccessedContent = 21,
    ChatServerInitiatedConnection = 22,
    ChatServerConnected = 23,
    ChatServerGreeting = 24,
    ChatServerReadyForPlay = 25,
    ChatServerAccessedContent = 26,
    ChatServerReadyForCommunication = 27
}

/// <summary>
/// The client object is the primary object used to communicate with the networking service
/// </summary>
public class Client
{  
    protected Client()
    {
        //Log.LogMessage += new LogMessageDelegate(Log_LogMessage);
        Factory.Instance.Register(typeof(CharacterInfo), delegate { return new CharacterInfo(); });
        m_Instance = this;
    }

    #region Events

    #region CentralServerDisconnected Event

    private ClusterServerDisconnectedDelegate CentralServerDisconnectedInvoker;

    /// <summary>
    /// Fires when the central server connection has been severed, for any reason.
    /// </summary>
    public event ClusterServerDisconnectedDelegate CentralServerDisconnected
    {
        add
        {
            AddHandler_CentralServerDisconnected(value);
        }
        remove
        {
            RemoveHandler_CentralServerDisconnected(value);
        }
    }

    
    private void AddHandler_CentralServerDisconnected(ClusterServerDisconnectedDelegate value)
    {
        CentralServerDisconnectedInvoker = (ClusterServerDisconnectedDelegate)Delegate.Combine(CentralServerDisconnectedInvoker, value);
    }
    
    private void RemoveHandler_CentralServerDisconnected(ClusterServerDisconnectedDelegate value)
    {
        CentralServerDisconnectedInvoker = (ClusterServerDisconnectedDelegate)Delegate.Remove(CentralServerDisconnectedInvoker, value);
    }

    private void FireCentralServerDisconnected(IClientConnection con, Client sender, string msg)
    {
        if (CentralServerDisconnectedInvoker != null)
        {
            CentralServerDisconnectedInvoker(con, sender, msg);
        }
    }

    #endregion

    #region GameServerDisconnected Event

    private ClusterServerDisconnectedDelegate GameServerDisconnectedInvoker;

    /// <summary>
    /// Fires when the game server connection has been severed, for any reason.
    /// </summary>
    public event ClusterServerDisconnectedDelegate GameServerDisconnected
    {
        add
        {
            AddHandler_GameServerDisconnected(value);
        }
        remove
        {
            RemoveHandler_GameServerDisconnected(value);
        }
    }

    
    private void AddHandler_GameServerDisconnected(ClusterServerDisconnectedDelegate value)
    {
        GameServerDisconnectedInvoker = (ClusterServerDisconnectedDelegate)Delegate.Combine(GameServerDisconnectedInvoker, value);
    }

    
    private void RemoveHandler_GameServerDisconnected(ClusterServerDisconnectedDelegate value)
    {
        GameServerDisconnectedInvoker = (ClusterServerDisconnectedDelegate)Delegate.Remove(GameServerDisconnectedInvoker, value);
    }

    private void FireGameServerDisconnected(IClientConnection con, Client sender, string msg)
    {
        if (GameServerDisconnectedInvoker != null)
        {
            GameServerDisconnectedInvoker(con, sender, msg);
        }
    }

    #endregion

    #region CentralServerReady Event

    private EventHandler CentralServerReadyInvoker;

    /// <summary>
    /// Fires when the central server sends the initial greeting after a connection has been made. 
    /// This event indicates that login and encryption exchange have all been concluded and the client
    /// is free to communicate with the central server.  
    /// </summary>
    public event EventHandler CentralServerReady
    {
        add
        {
            AddHandler_CentralServerReady(value);
        }
        remove
        {
            RemoveHandler_CentralServerReady(value);
        }
    }

    
    private void AddHandler_CentralServerReady(EventHandler value)
    {
        CentralServerReadyInvoker = (EventHandler)Delegate.Combine(CentralServerReadyInvoker, value);
    }

    
    private void RemoveHandler_CentralServerReady(EventHandler value)
    {
        CentralServerReadyInvoker = (EventHandler)Delegate.Remove(CentralServerReadyInvoker, value);
    }

    private void FireCentralServerReady(object sender, EventArgs args)
    {
        if (CentralServerReadyInvoker != null)
        {
            CentralServerReadyInvoker(sender, args);
        }
    }

    #endregion
   
    #region GameServerReady Event
    private EventHandler GameServerReadyInvoker;

    /// <summary>
    /// Fires when the game server sends the initial greeting after a connection has been made. 
    /// This event indicates that login and encryption exchange have all been concluded and the client
    /// is free to communicate with the central server.  
    /// </summary>
    public event EventHandler GameServerReady
    {
        add
        {
            AddHandler_GameServerReady(value);
        }
        remove
        {
            RemoveHandler_GameServerReady(value);
        }
    }

    
    private void AddHandler_GameServerReady(EventHandler value)
    {
        GameServerReadyInvoker = (EventHandler)Delegate.Combine(GameServerReadyInvoker, value);
    }

    
    private void RemoveHandler_GameServerReady(EventHandler value)
    {
        GameServerReadyInvoker = (EventHandler)Delegate.Remove(GameServerReadyInvoker, value);
    }

    private void FireGameServerReady(object sender, EventArgs args)
    {
        if (GameServerReadyInvoker != null)
        {
            GameServerReadyInvoker(sender, args);
        }
    }
    #endregion
   
    #region LoginServerTransferDirective Event
    protected ServerTransferDirectiveDelegate LoginServerTransferDirectiveInvoker;

    /// <summary>
    /// When the login server responds, letting us know if we can or can't log in
    /// </summary>
    public event ServerTransferDirectiveDelegate LoginServerTransferDirective
    {
        add
        {
            AddHandler_LoginServerTransferDirective(value);
        }
        remove
        {
            RemoveHandler_LoginServerTransferDirective(value);
        }
    }


    protected void AddHandler_LoginServerTransferDirective(ServerTransferDirectiveDelegate value)
    {
        LoginServerTransferDirectiveInvoker = (ServerTransferDirectiveDelegate)Delegate.Combine(LoginServerTransferDirectiveInvoker, value);
    }


    protected void RemoveHandler_LoginServerTransferDirective(ServerTransferDirectiveDelegate value)
    {
        LoginServerTransferDirectiveInvoker = (ServerTransferDirectiveDelegate)Delegate.Remove(LoginServerTransferDirectiveInvoker, value);
    }

    protected void FireLoginServerTransferDirective(INetworkConnection con, PacketGameServerTransferResult result)
    {
        if (LoginServerTransferDirectiveInvoker != null)
        {
            LoginServerTransferDirectiveInvoker(con, result);
        }
    }
    #endregion

    #region CentralServerTransferDirective Event
    private ServerTransferDirectiveDelegate CentralServerTransferDirectiveInvoker;

    /// <summary>
    /// When central sends us somewhere else in the cluster, this event fires.
    /// </summary>
    public event ServerTransferDirectiveDelegate CentralServerTransferDirective
    {
        add
        {
            AddHandler_CentralServerTransferDirective(value);
        }
        remove
        {
            RemoveHandler_CentralServerTransferDirective(value);
        }
    }

    
    private void AddHandler_CentralServerTransferDirective(ServerTransferDirectiveDelegate value)
    {
        CentralServerTransferDirectiveInvoker = (ServerTransferDirectiveDelegate)Delegate.Combine(CentralServerTransferDirectiveInvoker, value);
    }

    
    private void RemoveHandler_CentralServerTransferDirective(ServerTransferDirectiveDelegate value)
    {
        CentralServerTransferDirectiveInvoker = (ServerTransferDirectiveDelegate)Delegate.Remove(CentralServerTransferDirectiveInvoker, value);
    }

    private void FireCentralServerTransferDirective(INetworkConnection con, PacketGameServerTransferResult result)
    {
        if (CentralServerTransferDirectiveInvoker != null)
        {
            CentralServerTransferDirectiveInvoker(con, result);
        }
    }
    #endregion

    #region GameServerTransferDirective Event
    private ServerTransferDirectiveDelegate GameServerTransferDirectiveInvoker;

    /// <summary>
    /// When a game server bounces us to another server in the cluster, this event fires
    /// </summary>
    public event ServerTransferDirectiveDelegate GameServerTransferDirective
    {
        add
        {
            AddHandler_GameServerTransferDirective(value);
        }
        remove
        {
            RemoveHandler_GameServerTransferDirective(value);
        }
    }

    
    private void AddHandler_GameServerTransferDirective(ServerTransferDirectiveDelegate value)
    {
        GameServerTransferDirectiveInvoker = (ServerTransferDirectiveDelegate)Delegate.Combine(GameServerTransferDirectiveInvoker, value);
    }

    
    private void RemoveHandler_GameServerTransferDirective(ServerTransferDirectiveDelegate value)
    {
        GameServerTransferDirectiveInvoker = (ServerTransferDirectiveDelegate)Delegate.Remove(GameServerTransferDirectiveInvoker, value);
    }

    private void FireGameServerTransferDirective(INetworkConnection con, PacketGameServerTransferResult result)
    {
        if (GameServerTransferDirectiveInvoker != null)
        {
            GameServerTransferDirectiveInvoker(con, result);
        }
    }
    #endregion

    #region LoginServerDisconnected Event
    private EventHandler LoginServerDisconnectedInvoker;

    /// <summary>
    /// Fires when the login server gets disconnected.  This will also fire when we deliberately disconnect from 
    /// the login server as a result of the normal login process.
    /// </summary>
    public event EventHandler LoginServerDisconnected
    {
        add
        {
            AddHandler_LoginServerDisconnected(value);
        }
        remove
        {
            RemoveHandler_LoginServerDisconnected(value);
        }
    }

    
    private void AddHandler_LoginServerDisconnected(EventHandler value)
    {
        LoginServerDisconnectedInvoker = (EventHandler)Delegate.Combine(LoginServerDisconnectedInvoker, value);
    }

    
    private void RemoveHandler_LoginServerDisconnected(EventHandler value)
    {
        LoginServerDisconnectedInvoker = (EventHandler)Delegate.Remove(LoginServerDisconnectedInvoker, value);
    }

    private void FireLoginServerDisconnected(object sender, EventArgs args)
    {
        if (LoginServerDisconnectedInvoker != null)
        {
            LoginServerDisconnectedInvoker(sender, args);
        }
    }
    #endregion
  
    #region LoginServerResult Event
    private LoginServerOutboundConnection.LoginResultDelegate LoginServerResultInvoker;

    /// <summary>
    /// If login was successful, we request handoff
    /// to one of the clusters that the login server told us about in the welcome packet
    /// </summary>
    public event LoginServerOutboundConnection.LoginResultDelegate LoginServerResult
    {
        add
        {
            AddHandler_LoginServerResult(value);
        }
        remove
        {
            RemoveHandler_LoginServerResult(value);
        }
    }

    
    private void AddHandler_LoginServerResult(LoginServerOutboundConnection.LoginResultDelegate value)
    {
        LoginServerResultInvoker = (LoginServerOutboundConnection.LoginResultDelegate)Delegate.Combine(LoginServerResultInvoker, value);
    }

    
    private void RemoveHandler_LoginServerResult(LoginServerOutboundConnection.LoginResultDelegate value)
    {
        LoginServerResultInvoker = (LoginServerOutboundConnection.LoginResultDelegate)Delegate.Remove(LoginServerResultInvoker, value);
    }

    private void FireLoginServerResult(LoginServerOutboundConnection con, PacketLoginResult result)
    {
        if (LoginServerResultInvoker != null)
        {
            LoginServerResultInvoker(con, result);
        }
    }
    #endregion

    #region LoginServerLineSecured Event
    private EventHandler LoginServerLineSecuredInvoker;

    /// <summary>
    /// Fires when the login server completes its encryption key exchange.
    /// </summary>
    public event EventHandler LoginServerLineSecured
    {
        add
        {
            AddHandler_LoginServerLineSecured(value);
        }
        remove
        {
            RemoveHandler_LoginServerLineSecured(value);
        }
    }

    
    private void AddHandler_LoginServerLineSecured(EventHandler value)
    {
        LoginServerLineSecuredInvoker = (EventHandler)Delegate.Combine(LoginServerLineSecuredInvoker, value);
    }

    
    private void RemoveHandler_LoginServerLineSecured(EventHandler value)
    {
        LoginServerLineSecuredInvoker = (EventHandler)Delegate.Remove(LoginServerLineSecuredInvoker, value);
    }

    private void FireLoginServerLineSecured(object sender, EventArgs args)
    {
        if (LoginServerLineSecuredInvoker != null)
        {
            LoginServerLineSecuredInvoker(sender, args);
        }
    }
    #endregion

    #region CharacterListingArrived Event
    private EventHandler CharacterListingArrivedInvoker;

    /// <summary>
    /// Fires when we get a listing of characters from central server.  The actual character data will be stored in the
    /// Characters property for this object.
    /// </summary>
    public event EventHandler CharacterListingArrived
    {
        add
        {
            AddHandler_CharacterListingArrived(value);
        }
        remove
        {
            RemoveHandler_CharacterListingArrived(value);
        }
    }

    
    private void AddHandler_CharacterListingArrived(EventHandler value)
    {
        CharacterListingArrivedInvoker = (EventHandler)Delegate.Combine(CharacterListingArrivedInvoker, value);
    }

    
    private void RemoveHandler_CharacterListingArrived(EventHandler value)
    {
        CharacterListingArrivedInvoker = (EventHandler)Delegate.Remove(CharacterListingArrivedInvoker, value);
    }

    private void FireCharacterListingArrived(object sender, EventArgs args)
    {
        if (CharacterListingArrivedInvoker != null)
        {
            CharacterListingArrivedInvoker(sender, args);
        }
    }
    #endregion

    #region CharacterActivated Event
    private EventHandler CharacterActivatedInvoker;

    /// <summary>
    /// Fires when the server has acknowledged and activated for play the character we selected. The character data will be
    /// present in the CurrentCharacter property.
    /// </summary>
    public event EventHandler CharacterActivated
    {
        add
        {
            AddHandler_CharacterActivated(value);
        }
        remove
        {
            RemoveHandler_CharacterActivated(value);
        }
    }

    
    private void AddHandler_CharacterActivated(EventHandler value)
    {
        CharacterActivatedInvoker = (EventHandler)Delegate.Combine(CharacterActivatedInvoker, value);
    }

    
    private void RemoveHandler_CharacterActivated(EventHandler value)
    {
        CharacterActivatedInvoker = (EventHandler)Delegate.Remove(CharacterActivatedInvoker, value);
    }

    private void FireCharacterActivated(object sender, EventArgs args)
    {
        if (CharacterActivatedInvoker != null)
        {
            CharacterActivatedInvoker(sender, args);
        }
    }
    #endregion

    #region CreateCharacterFailed Event
    private Action<string> CreateCharacterFailedInvoker;

    /// <summary>
    /// Fires in response to a call to CreateCharacter.  Returns a message on failure
    /// </summary>
    public event Action<string> CreateCharacterFailed
    {
        add
        {
            AddHandler_CreateCharacterFailed(value);
        }
        remove
        {
            RemoveHandler_CreateCharacterFailed(value);
        }
    }

    
    private void AddHandler_CreateCharacterFailed(Action<string> value)
    {
        CreateCharacterFailedInvoker = (Action<string>)Delegate.Combine(CreateCharacterFailedInvoker, value);
    }

    
    private void RemoveHandler_CreateCharacterFailed(Action<string> value)
    {
        CreateCharacterFailedInvoker = (Action<string>)Delegate.Remove(CreateCharacterFailedInvoker, value);
    }

    private void FireCreateCharacterFailed(string msg)
    {
        if (CreateCharacterFailedInvoker != null)
        {
            CreateCharacterFailedInvoker(msg);
        }
    }
    #endregion
  
    #region SelectCharacterFailed Event
    private Action<string> SelectCharacterFailedInvoker;

    /// <summary>
    /// Fires in response to a call to SelectCharacter.  Returns a message on failure
    /// </summary>
    public event Action<string> SelectCharacterFailed
    {
        add
        {
            AddHandler_SelectCharacterFailed(value);
        }
        remove
        {
            RemoveHandler_SelectCharacterFailed(value);
        }
    }

    
    private void AddHandler_SelectCharacterFailed(Action<string> value)
    {
        SelectCharacterFailedInvoker = (Action<string>)Delegate.Combine(SelectCharacterFailedInvoker, value);
    }

    
    private void RemoveHandler_SelectCharacterFailed(Action<string> value)
    {
        SelectCharacterFailedInvoker = (Action<string>)Delegate.Remove(SelectCharacterFailedInvoker, value);
    }

    private void FireSelectCharacterFailed(string msg)
    {
        if (SelectCharacterFailedInvoker != null)
        {
            SelectCharacterFailedInvoker(msg);
        }
    }
    #endregion

    #region DeleteCharacterFailed Event
    private Action<string> DeleteCharacterFailedInvoker;

    /// <summary>
    /// Fires in response to a call to DeleteCharacter.  Returns a message on failure
    /// </summary>
    public event Action<string> DeleteCharacterFailed
    {
        add
        {
            AddHandler_DeleteCharacterFailed(value);
        }
        remove
        {
            RemoveHandler_DeleteCharacterFailed(value);
        }
    }

    
    private void AddHandler_DeleteCharacterFailed(Action<string> value)
    {
        DeleteCharacterFailedInvoker = (Action<string>)Delegate.Combine(DeleteCharacterFailedInvoker, value);
    }

    
    private void RemoveHandler_DeleteCharacterFailed(Action<string> value)
    {
        DeleteCharacterFailedInvoker = (Action<string>)Delegate.Remove(DeleteCharacterFailedInvoker, value);
    }

    private void FireDeleteCharacterFailed(string msg)
    {
        if (DeleteCharacterFailedInvoker != null)
        {
            DeleteCharacterFailedInvoker(msg);
        }
    }
    #endregion


    #endregion

    #region Data

    private bool m_CentralReadyForCommunication;
    /// <summary>
    /// True, if we have succesffully authenticated into the cenral service and are accepted by, logged in and ready to communicate
    /// </summary>
    public bool CentralReadyForCommunication
    {
        get { return m_CentralReadyForCommunication; }
        set { m_CentralReadyForCommunication = value; }
    }

    private bool m_GameServerReadyForPlay;
     /// <summary>
    /// True, if we have succesffully authenticated into have logged in to and have accessed the target content resource. We're ready to actually
    /// play, in other words.
    /// </summary>
    public bool GameServerReadyForPlay
    {
        get { return m_GameServerReadyForPlay; }
        set { m_GameServerReadyForPlay = value; }
    }
    
    /// <summary>
    /// All of the game servers that the login server has told us about.
    /// </summary>
    public GameServerInfo[] GameServers
    {
        get
        {
            if (m_LoginCon != null)
            {
                return m_LoginCon.GameServers.ToArray();
            }
            return new GameServerInfo[0];
        }
    }

    private ClientUser m_User = new ClientUser();
    /// <summary>
    /// The user represented by this client
    /// </summary>
    public ClientUser User
    {
        get { return m_User; }
        set { m_User = value; }
    }
    
    private static Client m_Instance;
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static Client Instance
    {
        get
        {
            if (m_Instance == null)
            {                
                m_Instance = new Client();
                //Log.LogMsg("*** CREATING NEW CLIENT OBJECT " + m_Instance.GetHashCode().ToString() + " ***");
            }

            //Log.LogMsg("*** RETURNING CLIENT OBJECT " + m_Instance.GetHashCode().ToString() + " ***");
            return m_Instance;
        }
    }

    private static ClientConnectionPhase m_ConnectionPhase;
    /// <summary>
    /// The current connection phase that the client is in.
    /// </summary>
    public static ClientConnectionPhase ConnectionPhase
    {
        get { return m_ConnectionPhase; }
        set { m_ConnectionPhase = value; }
    }

    private string m_TotalBytesRec;
    /// <summary>
    /// A user facing string, that describes the total number of bytes received thus far, via networking
    /// </summary>
    public string TotalBytesRec
    {
        get { return m_TotalBytesRec; }
    }

    private string m_TotalBytesSent;
    /// <summary>
    /// A user facing string, that describes the total number of bytes
    /// </summary>
    public string TotalBytesSent
    {
        get { return m_TotalBytesSent; }
    }

    private static long m_TotalLoginBytesSent = 0;
    private static long m_TotalLoginBytesRec = 0;
    private static long m_TotalGameServerBytesSent = 0;
    private static long m_TotalGameServerBytesRec = 0;

    /// <summary>
    /// Connection which handles the login server
    /// </summary>
    protected LoginServerOutboundConnection m_LoginCon;

    /// <summary>
    /// Connection which handles the game server
    /// </summary>
    protected ClientCentralServerOutboundConnection m_CentralServer;

    /// <summary>
    /// Connection which handles the game server
    /// </summary>
    protected ClientGameServerOutboundConnection m_GameServer;


    /// <summary>
    /// Do we have an active connection to the central server?
    /// </summary>
    public bool IsCentralServerConnected
    {
        get
        {
            return m_CentralServer != null && m_CentralServer.IsAlive;
        }
    }

    /// <summary>
    /// Do we have an active connection to the game server?
    /// </summary>
    public bool IsGameServerConnected
    {
        get
        {
            return m_GameServer != null && m_GameServer.IsAlive;
        }
    }

    /// <summary>
    /// Do we have an active connection to the login server?
    /// </summary>
    public bool IsLoginServerConnected
    {
        get
        {
            return m_LoginCon != null && m_LoginCon.IsAlive;
        }
    }

    #endregion

    #region Public Interface

    public virtual void Update(double deltaSecs)
    {
        INetworkConnection[] cons = new INetworkConnection[] { m_LoginCon, m_CentralServer, m_GameServer };
#if !SILVERLIGHT
        for (int i = 0; i < cons.Length; i++)
        {
            //Log.LogMsg("Cons[" + i.ToString() + "] == null? " + (cons[i] == null).ToString());
            if (cons[i] != null && cons[i].IsAlive && cons[i] is UnityClientConnection)
            {
                ((UnityClientConnection)cons[i]).Update(deltaSecs);
            }
        }
#endif
    }

    private bool m_ProcessNetworkingImmediately = true;
    public bool ProcessNetworkingImmediately 
    {
        get
        {
            return m_ProcessNetworkingImmediately;
        }
        set
        {
            m_ProcessNetworkingImmediately = value;
        }
    }

    /// <summary>
    /// Disconnects any login or game server that the client may be connected to
    /// </summary>
    public virtual void Disconnect()
    {
        INetworkConnection[] cons = new INetworkConnection[] { m_LoginCon, m_CentralServer, m_GameServer };
        
        for (int i = 0; i < cons.Length; i++)
        {
            //Log.LogMsg("Cons[" + i.ToString() + "] == null? " + (cons[i] == null).ToString());
            if (cons[i] != null)
            {
                cons[i].KillConnection("Shutting down.");
                cons[i].Dispose();
            }
        }
    }

    protected bool m_BlockingNetwork = false;

        /// <summary>
    /// To be called when the user initiates a manual login attempt.
    /// With this method, login server address and port number from the config file will be ignored.
    /// </summary>
    /// <param name="name">account name</param>
    /// <param name="pass">account password</param>
    /// <param name="loginServerAddress">login server address</param>
    /// <param name="loginServerPort">login server connection port</param>
    public bool ConnectToLoginServer(string name, string pass, bool newAccount, bool isBlocking, string loginServerAddress, int loginServerPort)
    {
        Disconnect();
        /*
        if (m_LoginCon != null && m_LoginCon.IsAlive)
        {
            m_LoginCon.KillConnection("Reconnecting.");
        }

        if (m_CentralServer != null && m_CentralServer.IsAlive)
        {
            m_CentralServer.KillConnection("Reconnecting.");
        }

        if (m_GameServer != null && m_GameServer.IsAlive)
        {
            m_GameServer.KillConnection("Reconnecting.");
        }
        */

        m_BlockingNetwork = isBlocking;
        SystemMessages.AddMessage(string.Format("Connecting to login server..."), SystemMessageType.Networking);
        User.AccountName = name;
        User.Password = pass;
        SetupLoginServerConnection(isBlocking);
        m_LoginCon.IsNewAccount = newAccount;
        
#if SILVERLIGHT
        m_LoginCon.BeginConnect(ConfigHelper.GetStringConfig("LoginServerAddress", "192.168.0.199"), ConfigHelper.GetIntConfig("LoginServerPort", 4502), name, pass);
#else
        if(loginServerAddress != null && loginServerAddress.Length > 0 && loginServerPort > 0)
        {
            m_LoginCon.BeginConnect(loginServerAddress, loginServerPort, name, pass);
        }
        else
        {
            m_LoginCon.BeginConnect(ConfigHelper.GetStringConfig("LoginServerAddress", "localhost"), ConfigHelper.GetIntConfig("LoginServerPort", 4502), name, pass);
        }
#endif
        Client.ConnectionPhase = ClientConnectionPhase.LoginServerInitiatedConnection;
        return true;
    }

    /// <summary>
    /// To be called when the user initiates a manual login attempt.
    /// With this method overload, the login server address and port number will
    /// be taken from the config file paramter "LoginServerAddress" and "LoginServerPort"
    /// </summary>
    /// <param name="name">account name</param>
    /// <param name="pass">account password</param>
    public bool ConnectToLoginServer(string name, string pass, bool newAccount, bool isBlocking)
    {
        return ConnectToLoginServer(name, pass, newAccount, isBlocking, null, -1);
    }

    /// <summary>
    /// Once the login server has authenticated us, it will send us a list of server clusters that it knows about.
    /// After the player has chosen a server cluster to play on (or perhaps the client auto-chooses one), this method
    /// can be called to petition the login server for handoff to that cluster.  The login server, in turn, will 
    /// petition the target cluster for the handoff.  Once the login server receives the result of the request, it 
    /// forwards it on to the player. If the petition is successful, the client will then possess an auth ticket which 
    /// can be used to connect to the target sub-server in the requested cluster.
    /// </summary>
    /// <param name="whichCluster">the cluster to request</param>
    /// <param name="targetResource">a specific resource we're interested in on the target cluster, if any.  helps route us to the correct sub-server that handles that resource</param>
    /// <returns>true, if the petition was dispatched, false if the target cluster is offline</returns>
    public bool PetitionLoginServerForClusterHandoff(GameServerInfo gsi, Guid targetResource)
    {
        if (gsi.IsOnline)
        {
            m_LoginCon.RequestHandoffToServer(gsi, targetResource);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Connect to a sub-server in the target cluster.  Connection info (address, port, auth ticket) comes from the central server.
    /// </summary>
    /// <returns>true, if the attempt kicked off successfully</returns>
    protected bool ConnectToGameServer(string serverName, string serverIP, int serverPort, Guid targetResource, Guid authTicket, PropertyBag parms, bool isBlocking, PacketLoginRequest.ConnectionType connectionType)
    {
        SetupGameServerConnection(isBlocking);
        m_GameServer.AuthTicket = User.AuthTicket = authTicket;
#if SILVERLIGHT
        m_GameServer.BeginConnect(serverIP, serverPort, User.AccountName, authTicket.ToString(), connectionType);
#else
        m_GameServer.BeginConnect(serverIP, serverPort, User.AccountName, authTicket.ToString(), connectionType);
#endif

        Client.ConnectionPhase = ClientConnectionPhase.WorldServerInitiatedConnection;
        return true;
    }

    /// <summary>
    /// Requests the creation of a new character.  If success, a new character listing including the new character will be sent from the server.  On failure, the CreateCharacterFailed
    /// event will fire.
    /// </summary>
    /// <param name="character">properties of the character to create</param>
    /// <param name="msg">failure message, if any</param>
    /// <returns></returns>
    public bool CreateCharacter(PropertyBag character, ref string msg)
    {
        msg = "";
        if (!IsCentralServerConnected || !CentralReadyForCommunication)
        {
            msg = "Not ready to communicate with central server.";
            return false;
        }

        return m_CentralServer.CreateCharacter(character);
    }

    /// <summary>
    /// Manually process networking packets. Must be called regularly (OnUpdate, for ex) if ProcessPacketsImmediately option is not set
    /// </summary>
    public virtual void ProcessNetworking()
    {
        //Log.LogMsg("Processing network. " + DateTime.Now.Ticks.ToString());
        INetworkConnection[] cons = new INetworkConnection[] { m_LoginCon, m_CentralServer, m_GameServer };

        for (int i = 0; i < cons.Length; i++)
        {
            if (cons[i] != null && cons[i].IsAlive)
                cons[i].ProcessNetworking();
        }
    }

    /// <summary>
    /// Selects a character for play.
    /// </summary>
    /// <param name="characterId">the id of the character to activate</param>
    /// <param name="msg"></param>
    /// <returns></returns>
    public bool SelectCharacter(int characterId, ref string msg)
    {
        msg = "";
        if (!IsCentralServerConnected || !CentralReadyForCommunication)
        {
            msg = "Not ready to communicate with central server.";
            return false;
        }

        return m_CentralServer.SelectCharacter(characterId);
    }

    /// <summary>
    /// Requests a complete listing of all characters on this server
    /// </summary>
    /// <param name="msg">a reply message, if any</param>
    /// <returns></returns>
    public bool RequestCharacterListing(ref string msg)
    {
        if (!IsCentralServerConnected || !CentralReadyForCommunication)
        {
            msg = "Not ready to communicate with central server.";
            return false;
        }

        return m_CentralServer.RequestCharacterListing();
    }

    /// <summary>
    /// Deletes a character on the server.  On success a new character listing, minus the target character will be sent.
    /// On failure, the DeleteCharacterFailed event will fire.
    /// </summary>
    /// <param name="character"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    public bool DeleteCharacter(int characterId, ref string msg)
    {
        msg = "";
        if (!IsCentralServerConnected || !CentralReadyForCommunication)
        {
            msg = "Not ready to communicate with central server.";
            return false;
        }

        return m_CentralServer.DeleteCharacter(characterId);
    }

    /// <summary>
    /// Connect to the central server in the target cluster.  Connection info (address, port, auth ticket) comes from the login server.
    /// </summary>
    /// <returns>true, if the attempt kicked off successfully</returns>
    protected bool ConnectToCentralServer(string serverName, string serverIP, int serverPort, Guid targetResource, Guid authTicket, bool isBlocking, PacketLoginRequest.ConnectionType connectionType)
    {
        SetupCentralServerConnection(isBlocking);
        m_CentralServer.AuthTicket = User.AuthTicket = authTicket;
#if SILVERLIGHT
        m_CentralServer.BeginConnect(serverIP, serverPort, User.AccountName, authTicket.ToString(), connectionType);
#else
        m_CentralServer.BeginConnect(serverIP, serverPort, User.AccountName, authTicket.ToString(), connectionType);
#endif

        Client.ConnectionPhase = ClientConnectionPhase.CentralServerInitiatedConnection;
        return true;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Disconnects and cleans up any current Login server connection and initizialzed a new one (m_LoginCon)
    /// </summary>
    protected virtual void SetupLoginServerConnection(bool isBlocking)
    {
        if (m_LoginCon != null)
        {
            if (m_LoginCon != null)
            {                
                if (m_LoginCon.IsAlive || m_LoginCon.ConnectionInProgress)
                {
                    m_LoginCon.KillConnection("Client requested disconnection.");
                }
            }

            //if (m_GameServer != null && (m_GameServer.IsAlive || m_GameServer.ConnectionInProgress))
            //{
            //    m_GameServer.KillConnection("Client requested disconnection.");
            //}

            //if (m_CentralServer != null && (m_CentralServer.IsAlive || m_CentralServer.ConnectionInProgress))
            //{
            //    m_CentralServer.KillConnection("Client requested disconnection.");
            //}

            m_LoginCon.SocketKilled -= new SocketKilledDelegate(OnLoginServer_Disconnected);
            m_LoginCon.LoginResolved -= new LoginServerOutboundConnection.LoginResultDelegate(OnLoginServer_AuthenticationResolved);
            m_LoginCon.ServerTransferDirective -= new ServerTransferDirectiveDelegate(OnLoginServer_ServerTransferResult);
            m_LoginCon.SocketSecured -= new EventHandler(OnLoginServer_LineSecuredResult);
#if !UNITY
            m_LoginCon.BeforeLoginRequest -= new ClientConnection.BeforeLoginRequestDelegate(OnLoginServer_BeforeLoginRequest);
#else
            m_LoginCon.BeforeLoginRequest -= new UnityClientConnection.BeforeLoginRequestDelegate(OnLoginServer_BeforeLoginRequest);
#endif
        }

        m_LoginCon = OnLoginServerConnectionCreate(isBlocking);
        m_LoginCon.ProcessIncomingPacketsImmediately = ProcessNetworkingImmediately;
        m_LoginCon.SocketKilled += new SocketKilledDelegate(OnLoginServer_Disconnected);
        m_LoginCon.LoginResolved += new LoginServerOutboundConnection.LoginResultDelegate(OnLoginServer_AuthenticationResolved);
        m_LoginCon.ServerTransferDirective += new ServerTransferDirectiveDelegate(OnLoginServer_ServerTransferResult);
        m_LoginCon.SocketSecured += new EventHandler(OnLoginServer_LineSecuredResult);
#if !UNITY
        m_LoginCon.BeforeLoginRequest += new ClientConnection.BeforeLoginRequestDelegate(OnLoginServer_BeforeLoginRequest);
#else
        m_LoginCon.BeforeLoginRequest += new UnityClientConnection.BeforeLoginRequestDelegate(OnLoginServer_BeforeLoginRequest);
#endif
        
    }

    void OnLoginServer_BeforeLoginRequest(PacketLoginRequest req)
    {
        OnBeforeLoginServerLoginRequest(req);
    }

    protected virtual void OnBeforeLoginServerLoginRequest(PacketLoginRequest req)
    {
    }

    /// <summary>
    /// Disconnects and cleans up any current game server connection and initizialzed a new one (m_GameServer).
    /// </summary>
    private void SetupCentralServerConnection(bool isBlocking)
    {
        CentralReadyForCommunication = false;
        if (m_CentralServer != null)
        {
            // Clean up current connection object
            //if (m_LoginCon != null && (m_LoginCon.IsAlive || m_LoginCon.ConnectionInProgress))
            //{
            //    m_LoginCon.KillConnection("Client requested disconnection.");
            //}

            //if (m_GameServer != null && (m_GameServer.IsAlive || m_GameServer.ConnectionInProgress))
            //{
            //    m_GameServer.KillConnection("Client requested disconnection.");
            //}

            if (m_CentralServer != null)
            {                
                if (m_CentralServer.IsAlive || m_CentralServer.ConnectionInProgress)
                {
                    m_CentralServer.KillConnection("Client requested disconnection.");
                }
            }

            m_CentralServer.SocketKilled -= new SocketKilledDelegate(OnCentralServer_SocketKilled);
            m_CentralServer.ServerReady -= new EventHandler(OnCentralServer_ServerReady);
            m_CentralServer.AuthTicketRejected -= new ClientServerOutboundConnection.AuthTicketRejectedDelegate(OnCentralServer_AuthTicketRejected);
            m_CentralServer.CharacterListingArrived -= new EventHandler(OnCentralServer_ChracterListingArrived);
            m_CentralServer.CharacterActivated -= new EventHandler(OnCentralServer_ChracterActivated);
            m_CentralServer.CreateCharacterFailed -= new Action<string>(OnCentralServer_CreateCharacterFailed);
            m_CentralServer.DeleteCharacterFailed -= new Action<string>(OnCentralServer_DeleteCharacterFailed);
            m_CentralServer.SelectCharacterFailed -= new Action<string>(OnCentralServer_SelectCharacterFailed);
            m_CentralServer.ServerTransferDirective -= new ServerTransferDirectiveDelegate(OnCentralServer_ServerTransferResult);
        }

        m_CentralServer = OnCentralServerConnectionCreate(isBlocking);
        m_CentralServer.ProcessIncomingPacketsImmediately = ProcessNetworkingImmediately;
        m_CentralServer.SocketKilled += new SocketKilledDelegate(OnCentralServer_SocketKilled);
        m_CentralServer.ServerReady += new EventHandler(OnCentralServer_ServerReady);
        m_CentralServer.AuthTicketRejected += new ClientServerOutboundConnection.AuthTicketRejectedDelegate(OnCentralServer_AuthTicketRejected);
        m_CentralServer.CharacterListingArrived += new EventHandler(OnCentralServer_ChracterListingArrived);
        m_CentralServer.CharacterActivated += new EventHandler(OnCentralServer_ChracterActivated);
        m_CentralServer.CreateCharacterFailed += new Action<string>(OnCentralServer_CreateCharacterFailed);
        m_CentralServer.DeleteCharacterFailed += new Action<string>(OnCentralServer_DeleteCharacterFailed);
        m_CentralServer.SelectCharacterFailed += new Action<string>(OnCentralServer_SelectCharacterFailed);
        m_CentralServer.ServerTransferDirective += new ServerTransferDirectiveDelegate(OnCentralServer_ServerTransferResult);        
    }

    protected virtual void OnCentralServer_ServerTransferResult(INetworkConnection con, PacketGameServerTransferResult result)
    {
        // Called by the central server connection in response to our request to be handed off to a game server.
        FireCentralServerTransferDirective(con, result);

        if (result.ReplyCode != ReplyType.OK)
        {
            SystemMessages.AddMessage(result.ReplyMessage, SystemMessageType.Networking);
            return;
        }
        
        Client.ConnectionPhase = ClientConnectionPhase.WorldServerInitiatedConnection;
        SystemMessages.AddMessage(string.Format("Connecting to game server '" + result.ServerName + "' as '" + User.AccountName + "' ..."), SystemMessageType.Networking);

        ConnectToGameServer(result.ServerName, result.ServerIP, result.ServerPort, result.TargetResource, result.AuthTicket, result.Parms,m_BlockingNetwork, result.IsAssistedTransfer? PacketLoginRequest.ConnectionType.AssistedTransfer : PacketLoginRequest.ConnectionType.UnassistedTransfer);
    }    

    /// <summary>
    /// Disconnects and cleans up any current game server connection and initizialzed a new one (m_GameServer).
    /// </summary>
    private void SetupGameServerConnection(bool isBlocking)
    {
        GameServerReadyForPlay  = false;
        if (m_GameServer != null)
        {
            //// Clean up current connection object
            //if (m_LoginCon != null && (m_LoginCon.IsAlive || m_LoginCon.ConnectionInProgress))
            //{
            //    m_LoginCon.KillConnection("Client requested disconnection.");
            //}

            if (m_GameServer != null)
            {                
                if (m_GameServer.IsAlive || m_GameServer.ConnectionInProgress)
                {
                    m_GameServer.KillConnection("Client requested disconnection.");
                }
            }

            //if (m_CentralServer != null && (m_CentralServer.IsAlive || m_CentralServer.ConnectionInProgress))
            //{
            //    m_CentralServer.KillConnection("Client requested disconnection.");
            //}

            m_GameServer.SocketKilled -= new SocketKilledDelegate(OnGameServer_SocketKilled);
            m_GameServer.ServerReady -= new EventHandler(OnGameServer_ServerReady);
            m_GameServer.AuthTicketRejected -= new ClientServerOutboundConnection.AuthTicketRejectedDelegate(OnGameServer_AuthTicketRejected);
            m_GameServer.ServerTransferDirective -= new ServerTransferDirectiveDelegate(OnGameServer_ServerTransferResult);
        }

        m_GameServer = OnGameServerConnectionCreate(isBlocking);
        OnAfterGameServerConnectionCreate(m_GameServer);
    }

    protected virtual void OnAfterGameServerConnectionCreate(ClientGameServerOutboundConnection con)
    {
        m_GameServer.ProcessIncomingPacketsImmediately = ProcessNetworkingImmediately;
        m_GameServer.SocketKilled += new SocketKilledDelegate(OnGameServer_SocketKilled);
        m_GameServer.ServerReady += new EventHandler(OnGameServer_ServerReady);
        m_GameServer.AuthTicketRejected += new ClientServerOutboundConnection.AuthTicketRejectedDelegate(OnGameServer_AuthTicketRejected);
        m_GameServer.ServerTransferDirective += new ServerTransferDirectiveDelegate(OnGameServer_ServerTransferResult);
    }

    protected virtual void OnGameServer_ServerTransferResult(INetworkConnection sender, PacketGameServerTransferResult accessInfo)
    {
        // Called by the game server connection in response to our request to be handed off to a game server.
        FireGameServerTransferDirective(sender, accessInfo);

        if (accessInfo.ReplyCode != ReplyType.OK)
        {
            SystemMessages.AddMessage(accessInfo.ReplyMessage, SystemMessageType.Networking);
            return;
        }

        Client.ConnectionPhase = ClientConnectionPhase.WorldServerInitiatedConnection;
        SystemMessages.AddMessage(string.Format("Connecting to Central server '" + accessInfo.ServerName + "' as '" + User.AccountName + "' ..."), SystemMessageType.Networking);

        ConnectToCentralServer(accessInfo.ServerName, accessInfo.ServerIP, accessInfo.ServerPort, accessInfo.TargetResource, accessInfo.AuthTicket, m_BlockingNetwork, accessInfo.IsAssistedTransfer ? PacketLoginRequest.ConnectionType.AssistedTransfer : PacketLoginRequest.ConnectionType.UnassistedTransfer);
    }    

    /// <summary>
    /// Override this to create and use your own derived version of a ClientGameServerOutboundConnection object.
    /// </summary>
    /// <returns>You must return a new instance of the object type you wish the Client to use</returns>
    protected virtual ClientGameServerOutboundConnection OnGameServerConnectionCreate(bool isBlocking)
    {
        return new ClientGameServerOutboundConnection(isBlocking);
    }

    /// <summary>
    /// Override this to create and use your own derived version of a ClientGameServerOutboundConnection object.
    /// </summary>
    /// <returns>You must return a new instance of the object type you wish the Client to use</returns>
    protected virtual ClientCentralServerOutboundConnection OnCentralServerConnectionCreate(bool isBlocking)
    {
        return new ClientCentralServerOutboundConnection(isBlocking);
    }

    /// <summary>
    /// Override this to create and use your own derived version of a ClientGameServerOutboundConnection object.
    /// </summary>
    /// <returns>You must return a new instance of the object type you wish the Client to use</returns>
    protected virtual LoginServerOutboundConnection OnLoginServerConnectionCreate(bool isBlocking)
    {
        return new LoginServerOutboundConnection(isBlocking);
    }

    /// <summary>
    /// Updates the text string which describes how much data we've downloaded from the server thus far
    /// </summary>
    public void UpdateNetworkBytesRec(long rec)
    {
        m_TotalBytesRec = string.Format("{0} MB down", Util.ConvertBytesToMegabytes(rec).ToString("0.000"));
    }

    /// <summary>
    /// Updates the text string which describes how much data we've uploaded from the server thus far
    /// </summary>
    public void UpdateNetworkBytesSent(long sent)
    {
        m_TotalBytesSent = string.Format("{0} MB up", Util.ConvertBytesToMegabytes(sent).ToString("0.000"));
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Gets called by the login server connection when the connection closes. This happens either automatically
    /// after the login server sends us our auth ticket for the game server (OnGameServerAccessPermissionArrived)
    /// or if something horrible happens during the authentication process.
    /// </summary>
    protected virtual void OnLoginServer_Disconnected(object loginConnection, string msg)
    {        
        SystemMessages.AddMessage(string.Format("Disconnected from login server. " + msg), SystemMessageType.Networking);
        if (!IsCentralServerConnected)
        {
            Client.ConnectionPhase = ClientConnectionPhase.Unconnected;
        }

        FireLoginServerDisconnected(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called when the login server has completed the encryption key exchange.
    /// </summary>
    protected virtual void OnLoginServer_LineSecuredResult(object sender, EventArgs args)
    {
        FireLoginServerLineSecured(sender, args);
    }

    protected virtual void ReadProfileFromLoginPacket(PacketLoginResult lr)
    {
        User.Roles = lr.Parms.GetStringArrayProperty(-1);
        User.MaxCharacters = lr.Parms.GetIntProperty(-2).GetValueOrDefault(1);
    }

    /// <summary>
    /// Called by the login server when the results to our login attempt are available
    /// </summary>
    protected virtual void OnLoginServer_AuthenticationResolved(LoginServerOutboundConnection loginConnection, PacketLoginResult lr)
    {
        //System.Diagnostics.Debug.WriteLine("Game Servers Available: " + m_LoginCon.GameServers.Count);
        Log.LogMsg("Game Servers Available: " + m_LoginCon.GameServers.Count);
        SystemMessages.AddMessage(string.Format("Login server authenticated: {0}.  {1}", lr.ReplyCode == ReplyType.OK, lr.ReplyMessage), SystemMessageType.Networking);

        if (lr.ReplyCode == ReplyType.OK)
        {
            ReadProfileFromLoginPacket(lr);
        }

        FireLoginServerResult(loginConnection, lr);
    }

    /// <summary>
    /// Called by the login server connection in response to our request to be handed off to a game server.
    /// </summary>
    /// <param name="accessInfo">information about our login attempt, including the result</param>
    protected virtual void OnLoginServer_ServerTransferResult(INetworkConnection con, PacketGameServerTransferResult result)
    {
        FireLoginServerTransferDirective(con, result);

        if (result.ReplyCode != ReplyType.OK)
        {
            SystemMessages.AddMessage(result.ReplyMessage,  SystemMessageType.Networking);
            return;
        }

        Client.ConnectionPhase = ClientConnectionPhase.LoginServerGotClusterServerAccess;
        SystemMessages.AddMessage(string.Format("Connecting to central server '" + result.ServerName + "' as '" + User.AccountName + "' ..."), SystemMessageType.Networking);

        ConnectToCentralServer(result.ServerName, result.ServerIP, result.ServerPort, result.TargetResource, result.AuthTicket, m_BlockingNetwork, result.IsAssistedTransfer ? PacketLoginRequest.ConnectionType.AssistedTransfer : PacketLoginRequest.ConnectionType.UnassistedTransfer);
    }

    protected virtual void OnCentralServer_ChracterListingArrived(object sender, EventArgs e)
    {
        SystemMessages.AddMessage(string.Format("Got Character listing."), SystemMessageType.System);
        User.Characters = m_CentralServer.Characters;
        FireCharacterListingArrived(this, e);
    }

    protected virtual void OnCentralServer_CreateCharacterFailed(string msg)
    {
        SystemMessages.AddMessage(string.Format("Character creation failed. " + msg), SystemMessageType.System);
        FireCreateCharacterFailed(msg);
    }

    protected virtual void OnCentralServer_DeleteCharacterFailed(string msg)
    {
        SystemMessages.AddMessage(string.Format("Character deletion failed."), SystemMessageType.System);
        FireDeleteCharacterFailed(msg);
    }

    protected virtual void OnCentralServer_SelectCharacterFailed(string msg)
    {
        SystemMessages.AddMessage(string.Format("Character selection failed."), SystemMessageType.System);
        FireSelectCharacterFailed(msg);
    }

    protected virtual void OnCentralServer_ChracterActivated(object sender, EventArgs e)
    {
        SystemMessages.AddMessage(string.Format("Character {0} Activated.", m_CentralServer.CurrentCharacter.CharacterName), SystemMessageType.System);
        User.CurrentCharacter = m_CentralServer.CurrentCharacter;
        FireCharacterActivated(this, e);
    }

    private int m_AuthAttempts = 0;
    protected virtual void OnCentralServer_AuthTicketRejected(string msg)
    {
        SystemMessages.AddMessage(string.Format("Central server refused authentication ticket [" + msg + "]. Acquiring new one from Login server."), SystemMessageType.Networking);
        Disconnect();
        m_AuthAttempts++;
        if (m_AuthAttempts >= 3)
        {
            m_AuthAttempts = 0;
            SystemMessages.AddMessage(string.Format("Attempted {0} times to submit an authentication ticket to Central server and was rejected each time.  Giving up. Try reconnecting later.", m_AuthAttempts), SystemMessageType.Networking);
            return;
        }
        ConnectToLoginServer(User.AccountName, User.Password, false,m_BlockingNetwork);
    }

    protected virtual void OnGameServer_AuthTicketRejected(string msg)
    {
        SystemMessages.AddMessage(string.Format("Game server refused authentication ticket [" + msg + "]. Acquiring new one from Login server."), SystemMessageType.Networking);
        Disconnect();
        m_AuthAttempts++;
        if (m_AuthAttempts >= 3)
        {
            m_AuthAttempts = 0;
            SystemMessages.AddMessage(string.Format("Attempted {0} times to submit an authentication ticket to Game server and was rejected each time.  Giving up. Try reconnecting later.", m_AuthAttempts), SystemMessageType.Networking);
            return;
        }
        ConnectToLoginServer(User.AccountName, User.Password, false,m_BlockingNetwork);
    }

    protected virtual void OnCentralServer_ServerReady(object sender, EventArgs e)
    {        
        m_AuthAttempts = 0;
        Client.ConnectionPhase = ClientConnectionPhase.CentralServerReadyForCommunication;
        CentralReadyForCommunication = true;

        FireCentralServerReady(this, EventArgs.Empty);

        INetworkConnection con = sender as INetworkConnection;        
        SystemMessages.AddMessage(string.Format("Ready to go on central server '" + con.RemoteEndPoint.ToString() + "'."), SystemMessageType.Networking);
        
        // m_LoginCon.KillConnection("Done with login server.  Thanks.");
        // login automatically D/Cs us
    }

    protected virtual void OnGameServer_ServerReady(object sender, EventArgs e)
    {
        // Central D/Cs itself on transfer to Content server
        // m_CentralServer.KillConnection("Transferred to game server.");

        m_AuthAttempts = 0;
        GameServerReadyForPlay = true;
        Client.ConnectionPhase = ClientConnectionPhase.WorldServerReadyForPlay;
        
        FireGameServerReady(this, EventArgs.Empty);

        INetworkConnection con = sender as INetworkConnection;        
        SystemMessages.AddMessage(string.Format("Ready to go on game server '" + con.RemoteEndPoint.ToString() + "'."), SystemMessageType.Networking);
    }

    protected virtual void OnCentralServer_SocketKilled(object sender, string msg)
    {
        CentralReadyForCommunication = false;
        IClientConnection con = sender as IClientConnection;
        SystemMessages.AddMessage(string.Format("Connection to central server closed. " + msg ), SystemMessageType.Networking);

        if (!IsGameServerConnected)
        {
            Client.ConnectionPhase = ClientConnectionPhase.Unconnected;
        }

        FireCentralServerDisconnected(con, this, msg);
    }

   protected virtual void  OnGameServer_SocketKilled(object sender, string msg)
    {
        GameServerReadyForPlay = false;
        INetworkConnection con = sender as INetworkConnection;
        SystemMessages.AddMessage(string.Format("Connection to game server " + con.RemoteEndPoint.ToString() + " closed."), SystemMessageType.Networking);
        Client.ConnectionPhase = ClientConnectionPhase.Unconnected;
        FireGameServerDisconnected((IClientConnection)con, this, msg);
    }

    /// <summary>
    /// Event handler to output log messages
    /// </summary>
    /// <param name="msg"></param>
    private void Log_LogMessage(string msg)
    {
        //System.Diagnostics.Debug.WriteLine(msg);
    }

    #endregion


    public ClientGameServerOutboundConnection GameServer
    {
        get
        {
            return m_GameServer;
        }
    }

    public ClientCentralServerOutboundConnection CentralServer
    {
        get
        {
            return m_CentralServer;
        }
    }

    public LoginServerOutboundConnection LoginServer
    {
        get
        {
            return m_LoginCon;
        }
    }

}