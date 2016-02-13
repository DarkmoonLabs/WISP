using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Configuration;
using System.IO;


/*
 <configuration>
  <configSections>
    <section name="ServerCommands" type="Shared.CommandConfigSection, ServerLib" />
  </configSections>
  <ServerCommands>
    <Commands>
      <add CommandName="RestartMachine" CommandGroup="Server" AllowedRoles="Administrator2" Assembly="Zeus.exe" ClassName="Shared.CommandRestartMachine" MethodName="Restart" ParmNames="" UsageHelp="Restarts the physical machine." />
      <add CommandName="Memand2" CommandGroup="Character" AllowedRoles="ActiveCustomerService" Assembly="" ClassName="TheClass" MethodName="MyMethod" ParmNames="Parm1|Parm2|Parm3" UsageHelp="Here is some help text" />
    </Commands>
  </ServerCommands>
 </configuration>
 */
namespace Shared
{
    public class CommandManager
    {
        public static void LoadFromConfig()
        {
            try
            {
                CommandConfigSection section = (CommandConfigSection)ConfigurationManager.GetSection("ServerCommands");
                if (section != null)
                {
                    foreach (CommandElement cmd in section.CommandItems)
                    {
                        CommandData c = new CommandData();
                        try
                        {
                            List<string> roles = new List<string>();
                            c.CommandName = cmd.CommandName;
                            string blob = cmd.AllowedRoles;
                            string[] parts = blob.Split(char.Parse("|"));
                            for (int i = 0; i < parts.Length; i++)
                            {
                                if (parts[i].Trim().Length > 0)
                                {
                                    roles.Add(parts[i].Trim());
                                }
                            }

                            if (roles.IndexOf("Administrator") == -1)
                            {
                                roles.Add("Administrator");
                            }
                            
                            c.AllowedRoles = roles.ToArray();
                            c.AssemblyName = cmd.Assembly;
                            c.ClassName = cmd.ClassName;                            
                            c.MethodName = cmd.MethodName;

                            List<string> parms = new List<string>();
                            blob = cmd.ParmNames;
                            parts = blob.Split(char.Parse("|"));
                            for (int i = 0; i < parts.Length; i++)
                            {
                                if (parts[i].Trim().Length > 0)
                                {
                                    parms.Add(parts[i].Trim());
                                }
                            }

                            c.ParmNames = parms.ToArray();
                            c.UsageHelp = cmd.UsageHelp;
                            c.CommandGroup = cmd.CommandGroup;
                            AddCommand(c);

                            Log1.Logger("Server.Commands").Info("Loaded data for server command [" + c.CommandName + "].");
                        }
                        catch (Exception e)
                        {
                            string cmdt = "";
                            if (c.CommandName != null && c.CommandName.Length > 0)
                            {
                                cmdt = c.CommandName;
                            }
                            else
                            {
                                cmdt = "Unknown Command Name";
                            }
                            Log1.Logger("Server.Commands").Error("Unable to read Server command from config file. [" + cmdt + "]. " + e.Message, e);
                        }
                    }
                }
            }
            catch (Exception fatl)
            {
                Log1.Logger("Server.Commands").Error("Unable to load Server commands from config file. " + fatl.Message, fatl);
            }
        }

        static CommandManager()
        {
            Commands = new Dictionary<string, CommandData>();
            CommandsByGroup = new Dictionary<string, List<CommandData>>();
        }

        public static Dictionary<string, CommandData> Commands { get; set; }

        public static Dictionary<string, List<CommandData>> CommandsByGroup { get; set; }

        /// <summary>
        /// Returns all commands in a given group
        /// </summary>
        /// <param name="group">the name of the group to return commands for. case sensitive.</param>
        /// <returns></returns>
        public static List<CommandData> GetCommands(string group)
        {            
            List<CommandData> cg = null;
            if (group == null || group.Length < 1)
            {
                cg = Commands.Values.ToList();
                return cg;
            }

            if (!CommandsByGroup.TryGetValue(group, out cg))
            {
                cg = new List<CommandData>();
            }

            return cg;
        }


        /// <summary>
        /// Adds a command to the list of known commands
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool AddCommand(CommandData c)
        {
            if (Commands.ContainsKey(c.CommandName))
            {
                return false;
            }

            Commands.Add(c.CommandName, c);

            if (c.CommandGroup == "" || c.CommandGroup == null)
            {
                c.CommandGroup = "General";
            }

            List<CommandData> cg = null;
            if (!CommandsByGroup.TryGetValue(c.CommandGroup, out cg))
            {
                cg = new List<CommandData>();
                CommandsByGroup.Add(c.CommandGroup, cg);
            }
            cg.Add(c);

            return true;
        }

        private static bool CanExecute(string[] RolesHave, string executor, CommandData c, string[] parms, ref string msg)
        {            
            if (Array.IndexOf(RolesHave, "Administrator") < 0)
            {
                bool allowed = false;
                for (int i = 0; i < c.AllowedRoles.Length; i++)
                {
                    if (Array.IndexOf(RolesHave, c.AllowedRoles[i]) > -1)
                    {
                        allowed = true;
                        break;
                    }
                }

                if (!allowed)
                {
                    Log1.Logger("Server.Commands").Warn("[" +executor + "] attempted executing command [" + c.CommandName + "] without adequate permissions.");                    
                    msg = "Not enough permissions for that command.";
                    return false;
                }

            }

            if (parms.Length == 0 || parms.Length-1 != c.ParmNames.Length)
            {
                msg = "Not enough parameters. " + c.UsageHelp;
                return false;
            }
            return true;
        }

        private static bool CanExecute(ServerUser user, CommandData c, string[] parms, ref string msg)
        {
            return CanExecute(user.Profile.UserRoles, user.AccountName, c, parms, ref msg);
        }

        private static Dictionary<string, Assembly> LoadedAssemblies = new Dictionary<string, Assembly>();

        private static Assembly GetAssembly(string name)
        {
            if (name == null || name.Length < 1)
            {
                return Assembly.GetCallingAssembly();
            }

            // Get entry assembly - When running as windows service, weird directory voodoo happens with System32 directory
            string directory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            name = Path.Combine(directory, name);

            Assembly ass = null;
            if (LoadedAssemblies.TryGetValue(name, out ass))
            {
                return ass;
            }

            try
            {                
                ass = Assembly.LoadFrom(name);
                if (ass != null)
                {
                    LoadedAssemblies.Add(name, ass);
                }
                return ass;
            }
            catch (Exception e)
            {
                Log1.Logger("Server.Commands").Error("Tried loading assembly [" + name + "] and failed. " + e.Message, e);
                return null;
            }
        }

        /// <summary>
        /// Executes a previously added command, passing the parms in order. Returns a textual response to the command.
        /// </summary>
        /// <param name="commandName">the name of the command to execute</param>
        /// <param name="parms">the paramters to pass</param>
        /// <returns></returns>
        public static string ExecuteCommand(ServerUser con, string commandName, string[] parms)
        {
            return ExecuteCommand(con.AccountName, con.Profile.UserRoles, commandName, parms);
        }

        private static string UnrollArray(string[] dat)
        {
            string pms = "";
            if (dat != null)
            {
                for (int i = 0; i < dat.Length; i++)
                {
                    pms += dat[i] + ", ";
                }
                pms.Trim().TrimEnd(char.Parse(","));
                pms = pms.Trim();

            }

            return pms;
        }

        public static string ExecuteCommand(string executor, string[] roles, string commandName, string[] parms)
        {
            try
            {
                string res = "";
                CommandData c = null;
                Array.Resize(ref parms, parms.Length + 1);
                parms[parms.Length-1] = executor;

                if (!Commands.TryGetValue(commandName, out c))
                {
                    res = "Unknown command.";
                }
                else
                {
                    if (!CanExecute(roles, executor, c, parms, ref res))
                    {
                        Log1.Logger("Server.Commands").Info("[" + executor + "] failed to execute [" + commandName + "(" + UnrollArray(parms) + ") = " + res + "]");
                        return res;
                    }

                    Assembly asm = GetAssembly(c.AssemblyName);
                    if (asm == null)
                    {
                        res = "Unable to find the assembly containing the command. Not executing.";
                        Log1.Logger("Server.Commands").Info("[" + executor + "] failed to execute [" + commandName + "(" + UnrollArray(parms) + ") = " + res + "]");
                        return res;
                    }

                    Type t =  asm.GetType(c.ClassName, false, true);
                    if (t == null)
                    {
                        res = "Unable to find class [" + c.ClassName + "] in assembly " + c.AssemblyName;
                        Log1.Logger("Server.Commands").Info("[" + executor + "] failed to execute [" + commandName + "(" + UnrollArray(parms) + ") = " + res + "]");
                        return res;
                    }

                    MethodInfo method = t.GetMethod(c.MethodName);
                    if (method == null)
                    {
                        res = "Unable to find method name [" + c.MethodName + "] in class [" + c.ClassName + "]. Not executing.";
                        Log1.Logger("Server.Commands").Info("[" + executor + "] failed to execute [" + commandName + "(" + UnrollArray(parms) + ") = " + res + "]");
                        return res;
                    }

                    ConstructorInfo ci = t.GetConstructor(Type.EmptyTypes);
                    if (ci == null)
                    {
                        res = "Unable to find default constructor for class [" + c.ClassName + "]. Not executing.";
                        Log1.Logger("Server.Commands").Info("[" + executor + "] failed to execute [" + commandName + "(" + UnrollArray(parms) + ") = " + res + "]");
                        return res;
                    }
                    object responder = ci.Invoke(null);
                    object result = method.Invoke(responder, parms);                    
                    string rslt = "";
                    if (result != null)
                    {
                        rslt = result.ToString();
                    }
                    else
                    {
                        rslt =  "Executed!";
                    }

                    Log1.Logger("Server.Commands").Info("[" + executor + "] executed [" + commandName + "(" + UnrollArray(parms) + ") = " + rslt + "]");
                    return rslt;
                }
                return res;
            }
            catch (Exception e)
            {
                return "Error executing command [" + commandName +  "]. " + e.Message;
            }
        }

    }
}
