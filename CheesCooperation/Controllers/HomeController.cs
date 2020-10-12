using CheesCooperation.Logic;
using CheesCooperation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CheesCooperation.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			var games = Games.ActualGames.Where(x => !x.Value.IsHidden).Select(x => new MenuModel()
			{
				Board = x.Value.ActualGameState.BoardPiecesChars,
				CanJoin = !x.Value.HasBothPlayers,
				Name = x.Key
			});
			return View(games);
		}

		public ActionResult About()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		public ActionResult Contact()
		{
			ViewBag.Message = "Your contact page.";

			return View();
		}
	}
}