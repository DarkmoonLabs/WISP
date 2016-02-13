using System;

namespace Shared
{
	/// <summary>
	/// Handles the various math calculations in a D20 system using any special rules.
	/// </summary>
	public class D20Math
	{
		public D20Math()
		{
		}

		public static int RoundDieRoll(float num, DieRollType typ)
		{
			num = (int)Math.Floor(num);
			if(num < 0)
			{
				if(typ == DieRollType.Damage || typ == DieRollType.HitPoint)
				{
					// Hitpoint and damage die rolls always have a minimum of one
					num = 1;
				}
			}

			return (int)num;
		}
		
		public static int AddMultipliers(int[] nums, MultiplyType typ)
		{
			if(nums == null)
			{
				return 0;
			}

			int total = 0;
			switch(typ)
			{
				case MultiplyType.GenericAbstractValue:
				case MultiplyType.DieRoll:
				case MultiplyType.Modifier:
					for(int i = 0; i < nums.Length; i++)
					{
						if(nums[i] == 1)
						{
							continue;
						}

						total += nums[i] - 1;
					}
					break;						
				
				case MultiplyType.GenericRealWorldValue:
					for(int x = 0; x < nums.Length; x++)
					{
						total *= nums[x];
					}
					break;
			}
		
			return total;
		}
	}
}















