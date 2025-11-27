






using UnityEngine;

namespace Controller2k
{
	public static class Extensions
	{
		
		public static bool IsEqualToZero(this float floatA)
		{
			return Mathf.Abs(floatA) < Mathf.Epsilon;
		}

		
		public static bool NotEqualToZero(this float floatA)
		{
			return Mathf.Abs(floatA) > Mathf.Epsilon;
		}
	}
}