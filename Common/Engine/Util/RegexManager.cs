using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Assets
{
	/// <summary>
	/// Handles the various regular expressions that might be used in an application
	/// </summary>
	public class RegexManager
	{
		private RegexManager()
		{
		}

		static RegexManager()
		{
		}

		/// <summary>
		/// stores the Regex objects against a key
		/// </summary>
        private static Dictionary<string, Regex> m_Regexes = new Dictionary<string, Regex>();

		public static Regex GetRegex(string key)
		{
			if(m_Regexes.ContainsKey(key.ToLower()))
			{
				return (Regex) m_Regexes[key.ToLower()];
			}

			return null;
		}

		/// <summary>
		/// adds a regex to the regex store for the app
		/// </summary>
		/// <param name="r">the regex object to add</param>
		/// <param name="key">the key under which to store the regex</param>
		public static void AddRegex(Regex r, string key)
		{
			if(m_Regexes.ContainsKey(key.ToLower()))
			{
				m_Regexes.Remove(key.ToLower());
			}

			m_Regexes.Add(key.ToLower(), r);
		}

		/// <summary>
		/// adds a regex to the regex store
		/// </summary>
		/// <param name="r">the string to build the regular expression from</param>
		/// <param name="key">the key under which to store the newly created regex</param>
		/// <param name="opts">the options for the regex</param>
		public static bool AddRegex(string r, string key, RegexOptions opts)
		{
			try
			{
#if !SILVERLIGHT
				Regex re = new Regex(r, opts | RegexOptions.Compiled);
#else
                Regex re = new Regex(r, opts);
#endif
				AddRegex(re, key);
			}
			catch
			{
				return false;
			}			
			return true;
		}

		/// <summary>
		/// Adds a regex to the regex store
		/// </summary>
		/// <param name="r">the string to build the regex from</param>
		/// <param name="key">the key under which to store the regex</param>
		/// <returns></returns>
		public static bool AddRegex(string r, string key)
		{			
			return AddRegex(r, key, RegexOptions.None);
		}
	}
}
