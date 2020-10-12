using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

using Chess;
using CheesCooperation.Logic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace CheesCooperation.Hubs
{
	public class GameHub : Hub
	{
		static private ConcurrentDictionary<string, string> usersInRooms = new ConcurrentDictionary<string, string>();

		private void updateGameState(dynamic toUpdate, SingleGame game)
		{
			toUpdate.updateGameState(
					game.ActualGameState.ZippedState,
					game.SecondsLeft(),
					game.HasStarted);
		}

		public override Task OnConnected()
		{
			return base.OnConnected();
		}

		public override Task OnDisconnected(bool stopCalled)
		{
			string roomID;
			if (usersInRooms.TryRemove(Context.ConnectionId, out roomID))
			{
				string playerName = PlayerName(null);

				SingleGame game;
				if (Games.ActualGames.TryGetValue(roomID, out game))
					lock (game)
					{
						playerName = PlayerName(game);

						if (game.BlackPlayer != null && game.BlackPlayer.ConnectionID == Context.ConnectionId)
							game.BlackPlayer = null;

						if (game.WhitePlayer != null && game.WhitePlayer.ConnectionID == Context.ConnectionId)
							game.WhitePlayer = null;
					}

				AddToChat(
					roomID,
					playerName + " left the room",
					"");
			}

			return base.OnDisconnected(stopCalled);
		}

		public async Task JoinRoom(string roomName, bool player)
		{
			if (roomName != null)
			{
				await Groups.Add(Context.ConnectionId, roomName);
				usersInRooms.TryAdd(Context.ConnectionId, roomName);

				SingleGame game = null;
				Games.ActualGames.TryGetValue(roomName, out game);

				if (player && game != null)
				{
					lock (game)
					{
						Player playerData = new Player()
							{
								UserName = Context.User.Identity.Name,
								ConnectionID = Context.ConnectionId
							};

						if (game.WhitePlayer == null)
							game.WhitePlayer = playerData;
						else if (game.BlackPlayer == null)
							game.BlackPlayer = playerData;
						else
							player = false;

					}
				}

				AddToChat(
					roomName,
					PlayerName(game) + (player ? " joined the game" : " joined spectators"),
					"");

				if (player && game != null && game.HasBothPlayers)
					updateGameState(Clients.All, game);
				else
					updateGameState(Clients.Caller, game);
			}
		}

		private bool IsPlayer(SingleGame game, bool white)
		{
			Player neededPlayer = white ? game.WhitePlayer : game.BlackPlayer;

			if (neededPlayer == null)
				return false;
			else return
				neededPlayer.ConnectionID == Context.ConnectionId;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="game">Cannot be null</param>
		/// <returns></returns>
		private bool PlayersTurn(SingleGame game)
		{
			return game.HasStarted && IsPlayer(game, game.ActualGameState.Tour == ChessGameState.Color.White);
		}
		private string PlayerName(SingleGame game)
		{
			string add1 = "";
			string add2 = "";
			if (game != null)
			{
				if (IsPlayer(game, false))
				{ add1 = "♚ "; add2 = " ♚"; }
				if (IsPlayer(game, true))
				{ add1 = "♔ "; add2 = " ♔"; }
			}

			string name = "Guest";
			string proposedName = Context.RequestCookies.ContainsKey("user_name") ? Context.RequestCookies["user_name"].Value : null;
			if (proposedName != null && proposedName.Length > 0 && proposedName.Length < 15)
				name = proposedName;

			return
				add1 + name + add2;
		}

		public void StartMove(string gameName, int x, int y)
		{
			SingleGame game;

			if (gameName != null && Games.ActualGames.TryGetValue(gameName, out game))
			{
				if (PlayersTurn(game))
				{
					ChessGameState state = game.ActualGameState;

					if (state != null)
						Clients.Group(gameName).highLightFields(state.GetPossibleMoves(x, y));
				}
			}
		}

		public void MoveTo(string gameName, int x1, int y1, int x2, int y2)
		{
			SingleGame game;

			if (gameName != null && Games.ActualGames.TryGetValue(gameName, out game))
			{
				if (PlayersTurn(game))
				{
					lock (game)
					{
						if (!game.IsOver)
						{
							ChessGameState state = game.ActualGameState;
							ChessGameState nextState = state.GetNextMove(x1, y1, x2, y2);

							if (nextState != null)
							{
								AddToChat(
									gameName,
									"" +
									state.PieceSymbolAtPosition(x1, y1) +
									(char)('A' + x1) + (1 + y1) +
									" to " +
									nextState.PieceSymbolAtPosition(x2, y2) +
									(char)('A' + x2) + (1 + y2), "");

								game.ActualGameState = nextState;
								state = nextState;

								Games.ShudeleGameRemoval(
									gameName,
									game.UpdateTime,
									game.TourTime,
									"Timeout: " + (state.Tour == ChessGameState.Color.White ? "balck player won" : "white player won"));
							}

							updateGameState(Clients.Group(gameName), game);

							if (game.IsOver)
							{
								string status = "draw";
								if (game.ActualGameState.WhiteChecked.Value) status = "balck player won";
								if (game.ActualGameState.BlackChecked.Value) status = "white player won";

								AddToChat(
									gameName,
									"Game over: " + status, 
									"");
							}
						}
					}
				}
				else
				{
					updateGameState(Clients.Caller, game);
				}
			}
		}

		public void DragTo(string gameName, int ID, double x, double y)
		{
			SingleGame game;

			if (gameName != null && Games.ActualGames.TryGetValue(gameName, out game))
			{
				if (PlayersTurn(game))
					if (ID >= 0 &&
						ID < 32 &&
						x >= 0 &&
						x <= 100 &&
						y >= 0 &&
						y <= 100)
						Clients.OthersInGroup(gameName).dragTo(ID, x, y);
			}
		}

		private void AddToChat(string gameName, string name, string text)
		{
			Clients.Group(gameName).addToChat(
					name,
					DateTime.Now.ToString("H:mm:ss"),
					HttpUtility.HtmlEncode(text));
		}

		public void SendMessage(string gameName, string text)
		{
			if (text != null && text.Length > 0 && text.Length < 300)
			{
				SingleGame game = null;

				if (gameName != null)
					Games.ActualGames.TryGetValue(gameName, out game);

				AddToChat(
					gameName,
					PlayerName(game) + ":",
					text);
			}
		}
	}
}