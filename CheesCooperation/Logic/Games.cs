using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;

using Chess;

using Quartz;
using Quartz.Impl;
using Microsoft.AspNet.SignalR;
using CheesCooperation.Hubs;

namespace CheesCooperation.Logic
{
	public class Player
	{
		public string ConnectionID { get; set; }
		public string UserName { get; set; }
	}

	public class SingleGame
	{
		public SingleGame()
		{
			ActualGameState = new ChessGameState();
			IsOver = false;
		}
		private ChessGameState _actualGameState;
		public ChessGameState ActualGameState
		{
			get
			{
				return _actualGameState;
			}
			set
			{
				_actualGameState = value;
				ActualGameState.ComputePossibleMoves();
				UpdateTime = DateTime.Now;

				if (ActualGameState.PossibleMovesCount == 0)
					IsOver = true;
			}
		}

		public Player WhitePlayer { get; set; }
		public Player BlackPlayer { get; set; }

		public bool HasBothPlayers { get { return WhitePlayer != null && BlackPlayer != null; } }
		public bool IsOver { get; set; }
		public bool HasStarted { get { return HasBothPlayers || _actualGameState.TourNumber != 0; } }

		public bool IsHidden { get; set; }

		public DateTime UpdateTime { get; set; }

		public int TourTime { get; set; }

		public int SecondsLeft()
		{
			if (ActualGameState.TourNumber != 0)
				return (int)(UpdateTime.AddSeconds(TourTime) - DateTime.Now).TotalSeconds;
			else return -1;
		}
	}

	public static class Games
	{
		private static ConcurrentDictionary<string, SingleGame> _actualGames = new ConcurrentDictionary<string, SingleGame>();
		public static ConcurrentDictionary<string, SingleGame> ActualGames
		{
			get { return _actualGames; }
		}

		public static void ShudeleGameRemoval(string gameName, DateTime timeToCompare, int seconds, string removalMessage)
		{
			IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
			IJobDetail job = JobBuilder.Create<RemoveGameJob>()
				.Build();
			job.JobDataMap["GameName"] = gameName;
			job.JobDataMap["TimeToCompare"] = timeToCompare;
			job.JobDataMap["RemovalMessage"] = removalMessage;
			ITrigger trigger = TriggerBuilder.Create()
				.StartAt(DateTime.Now.Add(TimeSpan.FromSeconds(seconds)))
				.Build();
			scheduler.ScheduleJob(job, trigger);
		}
	}

	internal class RemoveGameJob : IJob
	{
		public string GameName { get; set; }
		public DateTime TimeToCompare { get; set; }
		public string RemovalMessage { get; set; }

		public void Execute(IJobExecutionContext context)
		{
			SingleGame singleGame;
			if (Games.ActualGames.TryGetValue(GameName, out singleGame))
			{
				lock (singleGame)
				{
					if (singleGame.UpdateTime == TimeToCompare)
					{
						singleGame.IsOver = true;

						SingleGame singleGameRemoved;
						Games.ActualGames.TryRemove(GameName, out singleGameRemoved);

						var hub = GlobalHost.ConnectionManager.GetHubContext<GameHub>();
						hub.Clients.Group(GameName).AddToChat(
								RemovalMessage,
								DateTime.Now.ToString("H:mm:ss"),
								"");
					}
				}
			}
		}
	}
}