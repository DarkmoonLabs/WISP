using System;
using System.Collections.Generic;
using System.Text;

public enum SystemMessageType
{
    System,
    Networking
}
public class SystemMessages
{
    static SystemMessages()
    {
        AddMessage(string.Format("Ready."), SystemMessageType.System);
    }

    /// <summary>
    /// Variable to store the messages, up to MaxMessageLog (per SystemMessageType)
    /// </summary>
    private static Dictionary<SystemMessageType, List<string>> Messages = new Dictionary<SystemMessageType, List<string>>();

    /// <summary>
    /// Maximum number of messages to keep, per SystemMessageType
    /// </summary>
    public static int MaxMessageLog = 250;

    /// <summary>
    /// Gets all the messages of a certain type
    /// </summary>
    public List<string> MessagesOfType(SystemMessageType type)
    {
        List<string> msgs = null;
        if (!Messages.TryGetValue(type, out msgs))
        {
            msgs = new List<string>();
            Messages.Add(type, msgs);
        }

        return msgs;
    }

    /// <summary>
    /// Add a message to the message store
    /// </summary>
    public static void AddMessage(string msg, SystemMessageType type)
    {
        //System.Diagnostics.Debug.WriteLine(type.ToString() + ": " + msg);
        List<string> msgs = null;
        if (!Messages.TryGetValue(type, out msgs))
        {
            msgs = new List<string>();
            Messages.Add(type, msgs);
        }

        msgs.Add(msg);
        while (msgs.Count > MaxMessageLog)
        {
            msgs.RemoveAt(0);
        }
    }

    /// <summary>
    /// The last message that stored for the given type
    /// </summary>
    public static string LastMessageOfType(SystemMessageType type)
    {
        if (Messages.Count < 1)
        {
            return "";
        }

        List<string> msgs = null;
        if (!Messages.TryGetValue(type, out msgs))
        {
            msgs = new List<string>();
            Messages.Add(type, msgs);
        }

        if (msgs.Count < 1)
        {
            return "";
        }

        return msgs[msgs.Count - 1];
    }

    /// <summary>
    /// Returns the number of messages of a certain type
    /// </summary>
    public static int CountForType(SystemMessageType type)
    {
        List<string> msgs = null;
        if (!Messages.TryGetValue(type, out msgs))
        {
            msgs = new List<string>();
            Messages.Add(type, msgs);
        }

        return msgs.Count;
    }
}
