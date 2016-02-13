using System;
using System.Collections.Generic;
using System.Text;
using Shared;

public class ClientUser : User
{
    private string m_Password;
    /// <summary>
    /// The password associated with this account
    /// </summary>
    public string Password
    {
        get { return m_Password; }
        set { m_Password = value; }
    }

    public string[] Roles { get; set; }

    public PropertyBag AddedProperties { get; set; }

    /// <summary>
    /// A list of all characters that we know about for this user
    /// </summary>
    public List<CharacterInfo> Characters { get; set; }

    /// <summary>
    /// The currently selected character for this user
    /// </summary>
    public CharacterInfo CurrentCharacter { get; set; }

    public ClientUser()
        : base()
    {
        AddedProperties = new PropertyBag();
    }

    public int MaxCharacters { get; set; }
}
