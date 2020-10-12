using CheesCooperation.Hubs;
using CheesCooperation.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Quartz;
using Quartz.Impl;

namespace CheesCooperation.Controllers
{
    public class GameController : Controller
    {
		private static Object playerLock = new Object();

		public ActionResult Game(Models.JoinGameModel joinGame)
		{
			if (ModelState.IsValid)
			{
				SingleGame game = null;
				if (Games.ActualGames.TryGetValue(joinGame.GameName, out game))
				{
					return View(new Models.GameModel()
					{
						PiecesStates = game.ActualGameState.ZippedState,
						GameName = joinGame.GameName,
						WantsToBePlayer = joinGame.Player
					});
				}
				else
				{
					return RedirectToAction("Index", "Home");
				}
			}
			else
			{
				return RedirectToAction("Index", "Home");
			}
		}

		[HttpGet]
		public ActionResult NewGame()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult NewGame(Models.NewGameModel newGame)
		{
			if (ModelState.IsValid)
			{
				string gameName = newGame.GameName;
				var newSingleGame = new SingleGame() { TourTime = newGame.TourTime, IsHidden = newGame.IsHidden };

				if (Games.ActualGames.TryAdd(gameName, newSingleGame))
				{
					Games.ShudeleGameRemoval(
						gameName, 
						newSingleGame.UpdateTime,
						60*60,
						"Timeout: not enough players joined within an hour.");

					return RedirectToAction("Game", new Models.JoinGameModel() { GameName = newGame.GameName, Player = true });
				}
				else
				{
					ModelState.AddModelError("GameName", "Game with that name already exists");
					return View();
				}
			}
			else
			{
				return View();
			}
		}
    }
}