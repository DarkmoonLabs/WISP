using System;
using System.Text;
using System.Text.RegularExpressions;
using Assets;

namespace Shared
{
	/// <summary>
	/// Summary description for DieRoll.
	/// </summary>
	public class DieRoll
	{
		
		//new Regex(
		//	@"(?:(?<Neg>[-\+]*)\s*(?<Mult1>\d+)\s*(?<die>[d])\s*(?<Mult2>\d+)\s*)*\s*(?:(?<ModNeg>[-+]*)\s*(?<Mod>\d+))*\s*$",
		//	RegexOptions.Compiled);

		/// <summary>
		/// Instantiates object
		/// </summary>
		public DieRoll()
		{
		}

		/// <summary>
		/// Instantiates a DieRoll obj.
		/// </summary>
		/// <param name="dieRollData">the string representing the dieroll</param>
		public DieRoll(string dieRollData)
		{			
			try
			{
				Match m = RegexManager.GetRegex("dieroll").Match(dieRollData);
				if(!m.Success)
				{
					throw new ArgumentException("Unable to interpret die roll data: " + dieRollData);
				}
							
				Multiplier1 = m.Groups["Mult1"].Value.Length > 0? int.Parse(m.Groups["Mult1"].Value) : 0;
				Multiplier2 = m.Groups["Mult2"].Value.Length > 0? int.Parse(m.Groups["Mult2"].Value) : 0;
				Modifier    = m.Groups["Mod"].Value.Length   > 0? int.Parse(m.Groups["ModNeg"].Value + m.Groups["Mod"].Value) : 0;
				Negative	= m.Groups["Neg"].Value      == "-"	? true : false;
			}
			catch(Exception parseExc)
			{
				throw new ArgumentException("Unable to interpret die roll data: " + dieRollData);
			}
		}

		/// <summary>
		/// Instantiates a Die Roll
		/// </summary>
		/// <param name="m1">the '2' in !2d5-1</param>
		/// <param name="m2">the '5' in !2d5-1</param>
		/// <param name="mod">the '-1' in !2d5-1</param>
		/// <param name="negative">should there be a "!" in front of the dieroll</param>
		public DieRoll(int m1, int m2, int mod, bool negative)
		{
			Multiplier1 = m1;
			Multiplier2 = m2;
			Modifier    = mod;
			Negative	= negative;
		}

		/// <summary>
		/// Instantiates a positive value Die Roll
		/// </summary>
		/// <param name="m1">the '2' in !2d5-1</param>
		/// <param name="m2">the '5' in !2d5-1</param>
		/// <param name="mod">the '-1' in !2d5-1</param>
		public DieRoll(int m1, int m2, int mod)
		{
			Multiplier1 = m1;
			Multiplier2 = m2;
			Modifier    = mod;
			Negative	= false;
		}

		// 1d8+0
		/// <summary>
		/// the '2' in !2d5-1
		/// </summary>
		public int  Multiplier1 = 0; // 1d

		/// <summary>
		/// the '5' in !2d5-1
		/// </summary>
		public int  Multiplier2 = 0; // 8

		/// <summary>
		/// the '-1' in !2d5-1
		/// </summary>
		public int  Modifier    = 0; // +0

		/// <summary>
		/// '!' in !2d5-1, exists?
		/// </summary>
		public bool Negative    = false;

		public int MinValue
		{
			get
			{
				return Multiplier1 + Modifier;
			}
		}

		public int MaxValue
		{
			get
			{
				return (Multiplier1 * Multiplier2) + Modifier;
			}
		}
		
		private static Random m_Random = new Random();
		public int ExecuteRoll()
		{
			int total = 0;
			for(int i = 0; i < Multiplier1; i++)
			{				
				total += m_Random.Next(1, Multiplier2+1);
			}

			return total;
		}

		public static int ExecutePercentRoll()
		{
			DieRoll tens = new DieRoll(1, 10, 0);
			DieRoll ones = new DieRoll(1, 10, 0);

			int tenVal = tens.ExecuteRoll();
			int oneVal = ones.ExecuteRoll();

			if(tenVal == 10 && oneVal == 10)
			{
				return 100;
			}
			
			if(tenVal == 10)
			{
				tenVal = 0;
			}
			
			if(oneVal == 10)
			{
				oneVal = 0;
			}

			return Int32.Parse(tenVal.ToString() + oneVal.ToString());
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			if(Multiplier1 > 0 && Multiplier2 > 0)
			{
				sb.AppendFormat("{0}{1}d{2}", Negative? "!" : "",Multiplier1.ToString(), Multiplier2.ToString());
				if(Modifier > 0)
				{
					sb.Append("+");
				}
			}

			sb.AppendFormat("{0}", Modifier.ToString("D"));

			return sb.ToString();
		}


	}
}
