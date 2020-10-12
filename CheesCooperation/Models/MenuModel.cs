using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheesCooperation.Models
{
	public class MenuModel
	{
		public char[,] Board { get; set; }
		public bool CanJoin { get; set; }
		public string Name { get; set; }
	}
}