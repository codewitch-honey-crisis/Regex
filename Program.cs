using System;
using System.Collections.Generic;

namespace Grimoire
{
	class Program
	{
		static void Main(string[] args)
		{
			var rg = new Range<char>('0', '9');
			foreach (char ch in rg) Console.WriteLine(ch);
		}
	}
}
