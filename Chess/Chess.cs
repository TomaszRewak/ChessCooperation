using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
	public partial class ChessGameState
	{
		#region Additional data construction
		public struct Pair
		{
			public Pair(int x, int y)
				: this()
			{
				X = x;
				Y = y;
			}
			public int X { get; set; }
			public int Y { get; set; }
		}

		public enum PieceType
		{
			King = 0,
			Queen = 1,
			Rook = 2,
			Bishop = 3,
			Knight = 4,
			Pawn = 5,
		}
		public enum Color
		{
			White = 0,
			Black = 1
		}

		public class SinglePiece
		{
			public SinglePiece(PieceType type, Color color, int id)
			{
				Piece = type;
				Color = color;
				ID = id;
			}

			public SinglePiece(SinglePiece previous)
			{
				Piece = previous.Piece;
				Color = previous.Color;
				ID = previous.ID;
			}

			public PieceType Piece { get; set; }
			public Color Color { get; set; }
			public int ID { get; set; }

			public int CharSymbol
			{
				get
				{
					return 9812 +
						(int)Piece +
						(Color == Color.White ? 0 : 6);
				}
			}
		}

		/// <summary>
		/// Class representing single square on the board and what it holds.
		/// </summary>
		class Square
		{
			private ChessGameState Game { get; set; }

			public Square(ChessGameState game)
			{
				Game = game;

				LastUpdate = 0;
			}

			public Square(ChessGameState game, Square previous)
			{
				Game = game;

				if (previous.Piece != null)
					Piece = new SinglePiece(previous.Piece);

				LastUpdate = previous.LastUpdate;
			}

			private SinglePiece _piece;
			public SinglePiece Piece
			{
				get { return _piece; }
				set { LastUpdate = Game.TourNumber; _piece = value; }
			}

			/// <summary>
			/// Indicates when last time this squere has been updated (somebody moved there or from there)
			/// </summary>
			public int LastUpdate { get; set; }

			/// <summary>
			/// Holds all possible moves to be make by this piece in this exact tour.
			/// Can be filled by calling ComputePossibleMoves on ChessGameState holding this square.
			/// </summary>
			public List<ChessGameState> PossibleMoves = new List<ChessGameState>();
		}
		#endregion

		public ChessGameState()
		{
			Tour = Color.White;
			TourNumber = 0;

			for (int i = 0; i < 8; i++)
				for (int j = 0; j < 8; j++)
					board[i, j] = new Square(this);

			setPieces();

			ComputePossibleMoves();

			WhiteChecked = BlackChecked = new Lazy<bool>();
		}

		private ChessGameState(ChessGameState previous)
		{
			for (int i = 0; i < 8; i++)
				for (int j = 0; j < 8; j++)
					board[i, j] = new Square(this, previous.board[i, j]);

			WhiteChecked = new Lazy<bool>(() => CheckForCheck(Color.White));
			BlackChecked = new Lazy<bool>(() => CheckForCheck(Color.Black));
		}

		/// <summary>
		/// Sets pieces to their initial state.
		/// Board has to be empty to do that correctly.
		/// </summary>
		private void setPieces()
		{
			for (int column = 0; column < 8; column++)
			{
				this[column, 1].Piece = new SinglePiece(PieceType.Pawn, Color.White, column);
				this[column, 6].Piece = new SinglePiece(PieceType.Pawn, Color.Black, column + 16);
			}

			this[0, 0].Piece = new SinglePiece(PieceType.Rook, Color.White, 8);
			this[7, 0].Piece = new SinglePiece(PieceType.Rook, Color.White, 9);
			this[0, 7].Piece = new SinglePiece(PieceType.Rook, Color.Black, 8 + 16);
			this[7, 7].Piece = new SinglePiece(PieceType.Rook, Color.Black, 9 + 16);

			this[1, 0].Piece = new SinglePiece(PieceType.Knight, Color.White, 10);
			this[6, 0].Piece = new SinglePiece(PieceType.Knight, Color.White, 11);
			this[1, 7].Piece = new SinglePiece(PieceType.Knight, Color.Black, 10 + 16);
			this[6, 7].Piece = new SinglePiece(PieceType.Knight, Color.Black, 11 + 16);

			this[2, 0].Piece = new SinglePiece(PieceType.Bishop, Color.White, 12);
			this[5, 0].Piece = new SinglePiece(PieceType.Bishop, Color.White, 13);
			this[2, 7].Piece = new SinglePiece(PieceType.Bishop, Color.Black, 12 + 16);
			this[5, 7].Piece = new SinglePiece(PieceType.Bishop, Color.Black, 13 + 16);

			this[3, 0].Piece = new SinglePiece(PieceType.Queen, Color.White, 14);
			this[3, 7].Piece = new SinglePiece(PieceType.Queen, Color.Black, 14 + 16);

			this[4, 0].Piece = new SinglePiece(PieceType.King, Color.White, 15);
			this[4, 7].Piece = new SinglePiece(PieceType.King, Color.Black, 15 + 16);
		}

		private ChessGameState generateStateForNextMove(Pair movedTo)
		{
			ChessGameState newGameState = new ChessGameState(this);

			if (Tour == Color.White)
				newGameState.Tour = Color.Black;
			else
				newGameState.Tour = Color.White;

			newGameState.TourNumber = TourNumber + 1;

			newGameState.MovedTo = movedTo;

			return newGameState;
		}

		private Square[,] board = new Square[8, 8];
		/// <summary>
		/// Encapsulation of the board that allows accessing squers by a-h and 1-8 indexing
		/// </summary>
		/// <param name="column">int value between 0 and 7 (reprasenting columns a-h)</param>
		/// <param name="row">int value between 0 and 7 (reprasenting rows 1-8)</param>
		/// <returns></returns>
		private Square this[int column, int row]
		{
			get { return board[column, row]; }
			set { board[column, row] = value; }
		}

		/// <summary>
		/// Indicates which players tour it is.
		/// </summary>
		public Color Tour { get; private set; }
		/// <summary>
		/// Provides the number of this tour (is closely related to Square::LastUpdate field).
		/// </summary>
		public int TourNumber { get; private set; }
	}
}
