using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CheesCooperation.Models
{
	public class JoinGameModel
	{
		[StringLength(20, MinimumLength = 2), Required]
		public string GameName { get; set; }
		public bool Player { get; set; }
		public JoinGameModel()
		{
			Player = false;
		}
	}
	public class NewGameModel
	{
		[StringLength(20, MinimumLength = 2, ErrorMessage = "Name Length should be in range of 5 and 20 characters"), Required, DisplayName("Game name")]
		public string GameName { get; set; }
		[Range(10, 300), Required, DisplayName("Tour time (seconds)")]
		public int TourTime { get; set; }
		[Required, DisplayName("Hidden"), DefaultValue(false)]
		public bool IsHidden { get; set; }
	}
	public class GameModel
	{
		public Chess.ChessGameState.ZippedPieceState[] PiecesStates { get; set; }
		public string GameName { get; set; }
		public bool WantsToBePlayer { get; set; }
		public bool IsHidden { get; set; }
	}
}