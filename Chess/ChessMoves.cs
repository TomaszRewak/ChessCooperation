using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
	public partial class ChessGameState
	{
		public Pair MovedTo { get; private set; }
		public int PossibleMovesCount { get; set; }

		public struct ZippedPieceState
		{
			public int Symbol { get; set; }
			public bool OnBoard { get; set; }
			public int X { get; set; }
			public int Y { get; set; }
			public bool CanMove { get; set; }
		}

		private ZippedPieceState[] GenerateZippedStates()
		{
			ZippedPieceState[] states = new ZippedPieceState[32];

			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					Square square = this[i, j];

					if (square.Piece != null)
					{
						states[square.Piece.ID].Symbol = square.Piece.CharSymbol;
						states[square.Piece.ID].OnBoard = true;
						states[square.Piece.ID].X = i;
						states[square.Piece.ID].Y = j;
						states[square.Piece.ID].CanMove = square.PossibleMoves.Count != 0;
					}
				}
			}

			return states;
		}
		private ZippedPieceState[] zippedState = null;
		public ZippedPieceState[] ZippedState
		{
			get
			{
				lock (this)
					if (zippedState == null)
						zippedState = GenerateZippedStates();
				return zippedState;
			}
		}

		public char[,] BoardPiecesChars
		{
			get
			{
				char[,] boardPiecesChars = new char[8, 8];
				for (int i = 0; i < 8; i++)
				{
					for (int j = 0; j < 8; j++)
					{
						boardPiecesChars[i, j] = this[i, j].Piece != null ? (char)this[i, j].Piece.CharSymbol : ' ';
					}
				}
				return boardPiecesChars;
			}
		}

		public char PieceSymbolAtPosition(int x, int y)
		{
			if (squareInBoard(x, y) && this[x, y].Piece != null)
			{
				return
					(char)(9812 +
					(int)this[x, y].Piece.Piece +
					(this[x, y].Piece.Color == Color.White ? 0 : 6));
			}
			else
			{
				return '_';
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x">In range of 0-7</param>
		/// <param name="y">In range of 0-7</param>
		/// <returns></returns>
		public Pair[] GetPossibleMoves(int x, int y)
		{
			List<Pair> list = new List<Pair>();

			if (squareInBoard(x, y) &&
				this[x, y].Piece != null)
			{
				foreach (var move in this[x, y].PossibleMoves)
				{
					list.Add(
						new Pair(
							move.MovedTo.X,
							move.MovedTo.Y));
				}
			}

			return list.ToArray();
		}

		public ChessGameState GetNextMove(int x1, int y1, int x2, int y2)
		{
			ChessGameState toReturn = null;

			if (squareInBoard(x1, y1) &&
				squareInBoard(x2, y2))
				toReturn = this[x1, y1].PossibleMoves.FirstOrDefault(
					(elem) => elem.MovedTo.X == x2 && elem.MovedTo.Y == y2);

			return toReturn;
		}

		private bool squareInBoard(int column, int row)
		{
			return
				column < 8 &&
				column >= 0 &&
				row < 8 &&
				row >= 0;
		}

		/// <summary>
		/// Compute all possible moves for all squares.
		/// </summary>
		public void ComputePossibleMoves()
		{
			PossibleMovesCount = 0;

			for (int column = 0; column < 8; column++)
			{
				for (int row = 0; row < 8; row++)
				{
					if (this[column, row].Piece != null &&
						this[column, row].Piece.Color == Tour)
					{
						ComputePossibleMoves(column, row);
						PossibleMovesCount += this[column, row].PossibleMoves.Count;
					}
				}
			}
		}

		/// <summary>
		/// Compute all possible moves for exact square.
		/// </summary>
		private void ComputePossibleMoves(int column, int row)
		{
			Action<int, int> moveDiagonaly = (int x, int y) =>
			{
				int c = column + x;
				int r = row + y;

				while (squareInBoard(c, r) &&
					this[c, r].Piece == null)
				{
					ChessGameState newState = generateStateForNextMove(new Pair(c, r));
					newState[c, r].Piece = newState[column, row].Piece;
					newState[column, row].Piece = null;
					if (!newState.IsChecked(Tour))
						this[column, row].PossibleMoves.Add(newState);

					c += x;
					r += y;
				}

				if (squareInBoard(c, r) &&
					this[c, r].Piece != null &&
					this[c, r].Piece.Color != Tour)
				{
					ChessGameState newState = generateStateForNextMove(new Pair(c, r));
					newState[c, r].Piece = newState[column, row].Piece;
					newState[column, row].Piece = null;
					if (!newState.IsChecked(Tour))
						this[column, row].PossibleMoves.Add(newState);
				}
			};

			Action<int, int> moveJump = (int x, int y) =>
			{
				int c = column + x;
				int r = row + y;

				if (squareInBoard(c, r) &&
					((this[c, r].Piece != null &&
					this[c, r].Piece.Color != Tour) ||
					this[c, r].Piece == null))
				{
					ChessGameState newState = generateStateForNextMove(new Pair(c, r));
					newState[c, r].Piece = newState[column, row].Piece;
					newState[column, row].Piece = null;
					if (!newState.IsChecked(Tour))
						this[column, row].PossibleMoves.Add(newState);
				}
			};

			switch (this[column, row].Piece.Piece)
			{
				case PieceType.Pawn:
					{
						int direction = 0;

						if (Tour == Color.White)
							direction = 1;
						else
							direction = -1;

						Action<PieceType> pawnPromote = (PieceType toPiece) =>
						{
							if (!squareInBoard(column, row + direction))
							{
								ChessGameState newState = generateStateForNextMove(new Pair(column, row));
								newState[column, row].Piece.Piece = toPiece;
								newState[column, row].LastUpdate = newState.TourNumber;
								if (!newState.IsChecked(Tour))
									this[column, row].PossibleMoves.Add(newState);
							}
						};

						Func<int, bool> pawnHasEnemyOnDiagonal = (int side) =>
						{
							return
								squareInBoard(column + side, row + direction) &&
								this[column + side, row + direction].Piece != null &&
								this[column + side, row + direction].Piece.Color != Tour;
						};

						Action<int> pawnBeat = (int side) =>
						{
							if (pawnHasEnemyOnDiagonal(side))
							{
								ChessGameState newState = generateStateForNextMove(new Pair(column + side, row + direction));
								newState[column + side, row + direction].Piece = newState[column, row].Piece;
								newState[column, row].Piece = null;
								if (!newState.IsChecked(Tour))
									this[column, row].PossibleMoves.Add(newState);
							}
						};

						Action pawnMove = () =>
						{
							if (pawnHasEnemyOnDiagonal(1) ||
								pawnHasEnemyOnDiagonal(-1))
								return;

							if (squareInBoard(column, row + direction) &&
								this[column, row + direction].Piece == null)
							{
								ChessGameState newState = generateStateForNextMove(new Pair(column, row + direction));
								newState[column, row + direction].Piece = newState[column, row].Piece;
								newState[column, row].Piece = null;
								if (!newState.IsChecked(Tour))
									this[column, row].PossibleMoves.Add(newState);

								// Double move
								if (this[column, row].LastUpdate == 0 &&
									squareInBoard(column, row + 2 * direction) &&
									this[column, row + 2 * direction].Piece == null)
								{
									ChessGameState newState2 = generateStateForNextMove(new Pair(column, row + 2 * direction));
									newState2[column, row + 2 * direction].Piece = newState2[column, row].Piece;
									newState2[column, row].Piece = null;
									if (!newState2.IsChecked(Tour))
										this[column, row].PossibleMoves.Add(newState2);
								}
							}
						};

						Action<int> pawnPassat = (int side) =>
						{

							if (squareInBoard(column + side, row) &&
								this[column + side, row].Piece != null &&
								this[column + side, row].Piece.Piece == PieceType.Pawn &&
								this[column + side, row].Piece.Color != Tour &&
								this[column + side, row].LastUpdate == TourNumber &&
								squareInBoard(column + side, row + 2 * direction) &&
								this[column + side, row + 2 * direction].Piece == null &&
								this[column + side, row + 2 * direction].LastUpdate == TourNumber)
							{
								ChessGameState newState = generateStateForNextMove(new Pair(column + side, row + direction));
								newState[column + side, row + direction].Piece = newState[column, row].Piece;
								newState[column, row].Piece = null;
								newState[column + side, row].Piece = null;
								if (!newState.IsChecked(Tour))
									this[column, row].PossibleMoves.Add(newState);
							}
						};

						pawnPromote(PieceType.Queen);
						pawnPromote(PieceType.Rook);
						pawnPromote(PieceType.Knight);
						pawnPromote(PieceType.Bishop);

						pawnBeat(-1);
						pawnBeat(1);

						pawnMove();

						pawnPassat(-1);
						pawnPassat(1);

						break;
					}
				case PieceType.Rook:
					{
						moveDiagonaly(1, 0);
						moveDiagonaly(-1, 0);
						moveDiagonaly(0, 1);
						moveDiagonaly(0, -1);
						break;
					}
				case PieceType.Bishop:
					{
						moveDiagonaly(1, 1);
						moveDiagonaly(-1, 1);
						moveDiagonaly(1, -1);
						moveDiagonaly(-1, -1);
						break;
					}
				case PieceType.Queen:
					{
						moveDiagonaly(1, 0);
						moveDiagonaly(-1, 0);
						moveDiagonaly(0, 1);
						moveDiagonaly(0, -1);

						moveDiagonaly(1, 1);
						moveDiagonaly(-1, 1);
						moveDiagonaly(1, -1);
						moveDiagonaly(-1, -1);
						break;
					}
				case PieceType.Knight:
					{
						moveJump(1, 2);
						moveJump(2, 1);
						moveJump(2, -1);
						moveJump(1, -2);
						moveJump(-1, -2);
						moveJump(-2, -1);
						moveJump(-2, 1);
						moveJump(-1, 2);
						break;
					}
				case PieceType.King:
					{
						Action castlingLeft = () =>
						{

							if (this[column, row].LastUpdate == 0 &&
								this[0, row].Piece != null &&
								this[0, row].LastUpdate == 0 &&
								this[1, row].Piece == null &&
								this[2, row].Piece == null &&
								this[3, row].Piece == null)
							{
								ChessGameState newState = generateStateForNextMove(new Pair(2, row));
								newState[2, row].Piece = newState[4, row].Piece;
								newState[4, row].Piece = null;
								ChessGameState newCrossState = generateStateForNextMove(new Pair(3, row));
								newState[3, row].Piece = newState[4, row].Piece;
								newState[3, row].Piece = null;
								if (!this.IsChecked(Tour) &&
									!newState.IsChecked(Tour) &&
									!newCrossState.IsChecked(Tour))
								{
									newState[3, row].Piece = newState[0, row].Piece;
									newState[0, row].Piece = null;

									this[column, row].PossibleMoves.Add(newState);
								}
							}
						};

						Action castlingRight = () =>
						{

							if (this[column, row].LastUpdate == 0 &&
								this[7, row].Piece != null &&
								this[7, row].LastUpdate == 0 &&
								this[5, row].Piece == null &&
								this[6, row].Piece == null)
							{
								ChessGameState newState = generateStateForNextMove(new Pair(6, row));
								newState[6, row].Piece = newState[4, row].Piece;
								newState[4, row].Piece = null;
								ChessGameState newCrossState = generateStateForNextMove(new Pair(5, row));
								newState[5, row].Piece = newState[4, row].Piece;
								newState[4, row].Piece = null;
								if (!this.IsChecked(Tour) &&
									!newState.IsChecked(Tour) &&
									!newCrossState.IsChecked(Tour))
								{
									newState[5, row].Piece = newState[7, row].Piece;
									newState[7, row].Piece = null;

									this[column, row].PossibleMoves.Add(newState);
								}
							}
						};

						moveJump(1, 1);
						moveJump(1, 0);
						moveJump(1, -1);
						moveJump(0, -1);
						moveJump(-1, -1);
						moveJump(-1, 0);
						moveJump(-1, 1);
						moveJump(0, 1);
						castlingLeft();
						castlingRight();
						break;
					}
			}
		}

		public Lazy<bool> WhiteChecked { get; private set; }
		public Lazy<bool> BlackChecked { get; private set; }
		public bool IsChecked(Color color)
		{
			if (color == Color.White)
				return WhiteChecked.Value;
			else
				return BlackChecked.Value;
		}
		private bool CheckForCheck(Color color)
		{
			for (int column = 0; column < 8; column++)
			{
				for (int row = 0; row < 8; row++)
				{
					if (this[column, row].Piece != null &&
						this[column, row].Piece.Piece == PieceType.King &&
						this[column, row].Piece.Color == color)
					{
						int direction;

						if (color == Color.White)
							direction = 1;
						else
							direction = -1;

						Func<int, int, PieceType, PieceType, bool> onDiagonal = (int a, int b, PieceType pieceA, PieceType pieceB) =>
						{
							int c = column + a;
							int r = row + b;

							while (squareInBoard(c, r) &&
								this[c, r].Piece == null)
							{
								c += a;
								r += b;
							}

							return
								squareInBoard(c, r) &&
								this[c, r].Piece != null &&
								this[c, r].Piece.Color != color &&
								(
									this[c, r].Piece.Piece == pieceA ||
									this[c, r].Piece.Piece == pieceB
								);
						};

						Func<int, int, PieceType, bool> onJump = (int a, int b, PieceType piece) =>
						{
							return
								squareInBoard(column + a, row + b) &&
								this[column + a, row + b].Piece != null &&
								this[column + a, row + b].Piece.Color != color &&
								this[column + a, row + b].Piece.Piece == piece;
						};

						return
							onJump(1, direction, PieceType.Pawn) ||
							onJump(-1, direction, PieceType.Pawn) ||

							onDiagonal(1, 1, PieceType.Queen, PieceType.Bishop) ||
							onDiagonal(1, -1, PieceType.Queen, PieceType.Bishop) ||
							onDiagonal(-1, 1, PieceType.Queen, PieceType.Bishop) ||
							onDiagonal(-1, -1, PieceType.Queen, PieceType.Bishop) ||

							onDiagonal(1, 0, PieceType.Queen, PieceType.Rook) ||
							onDiagonal(-1, 0, PieceType.Queen, PieceType.Rook) ||
							onDiagonal(0, 1, PieceType.Queen, PieceType.Rook) ||
							onDiagonal(0, -1, PieceType.Queen, PieceType.Rook) ||

							onJump(1, 2, PieceType.Knight) ||
							onJump(2, 1, PieceType.Knight) ||
							onJump(2, -1, PieceType.Knight) ||
							onJump(1, -2, PieceType.Knight) ||
							onJump(-1, -2, PieceType.Knight) ||
							onJump(-2, -1, PieceType.Knight) ||
							onJump(-2, 1, PieceType.Knight) ||
							onJump(-1, 2, PieceType.Knight) ||

							onJump(1, 1, PieceType.King) ||
							onJump(1, -1, PieceType.King) ||
							onJump(-1, 1, PieceType.King) ||
							onJump(-1, -1, PieceType.King) ||
							onJump(1, 0, PieceType.King) ||
							onJump(-1, 0, PieceType.King) ||
							onJump(0, 1, PieceType.King) ||
							onJump(0, -1, PieceType.King);
					}
				}
			}

			return false;
		}
	}
}
