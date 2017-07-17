﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

class TheGreatEscapeReferee
{
	public static void Main(string[] args)
	{
		string path = args.Length == 1 ? args[0] : null;

		int playerCount = int.Parse(Console.ReadLine().Split()[1]);

		Board board = new Board(playerCount);
		if (path != null)
		{
			Bitmap bmp = board.Draw();
			bmp.Save(path + System.IO.Path.DirectorySeparatorChar + "000.png");
			bmp.Dispose();
		}

		while (board.Play())
		{
			board.Tick(path);
		}
		board.DeclareWinner();
	}

	class Board
	{
		public static readonly int SIZE = 9;
		public static readonly int MAX_ROUNDS = 100;

		private static Random random = new Random();
		private List<Player> activePlayers = new List<Player>();
		private List<Player> finishedPlayers = new List<Player>();
		private int playerCount;
		public int Round { get; private set; }

		private Field[,] Grid;
		private bool[,,] Walls;
		private List<Wall> wallList = new List<Wall>();
		private static int hor = 0;
		private static int ver = 1;
		private static int[,] offset = new int[,] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };
		private static string[] directions = new string[] { "DOWN", "RIGHT", "UP", "LEFT" };

		public Board(int playerCount)
		{
			this.playerCount = playerCount;
			for (int i = 0; i < playerCount; i++)
			{
				activePlayers.Add(new Player(i, random, playerCount));
			}

			this.Walls = new bool[SIZE + 1, SIZE + 1, 2];
			Grid = new Field[SIZE, SIZE];
			for (int x = 0; x < SIZE; x++)
			{
				for (int y = 0; y < SIZE; y++)
				{
					Grid[x, y] = new Field(x, y);
				}
			}
			for (int x = 0; x < SIZE; x++)
			{
				for (int y = 0; y < SIZE; y++)
				{
					for (int dir = 0; dir < 4; dir++)
					{
						int x_ = x + offset[dir, 0];
						int y_ = y + offset[dir, 1];
						if (x_ >= 0 && x_ < SIZE && y_ >= 0 && y_ < SIZE)
							Grid[x, y].Neighbors.Add(Grid[x_, y_]);
					}
				}
			}
		}

		public bool AddWall(Wall w)
		{
			if (Walls[w.X, w.Y, w.Horizontal ? hor : ver])
				return false;
			if (w.Horizontal)
			{
				if (w.X < 0 || w.X >= 8 || w.Y <= 0 || w.Y > 8) //out of bounds
					return false;
				if (w.X > 0 && Walls[w.X - 1, w.Y, hor] || Walls[w.X + 1, w.Y, hor]) //blocked in same direction
					return false;
				if (w.Y > 0 && Walls[w.X + 1, w.Y - 1, ver]) //cross
					return false;
			}
			else
			{
				if (w.X <= 0 || w.X > 8 || w.Y < 0 || w.Y >= 8) //out of bounds
					return false;
				if (w.Y > 0 && Walls[w.X, w.Y - 1, ver] || Walls[w.X, w.Y + 1, ver]) //blocked in same direction
					return false;
				if (w.X > 0 && Walls[w.X - 1, w.Y + 1, hor]) //cross
					return false;
			}
			Walls[w.X, w.Y, w.Horizontal ? hor : ver] = true;
			if (w.Horizontal)
			{
				Field topLeft = Grid[w.X, w.Y - 1];
				Field topRight = Grid[w.X + 1, w.Y - 1];
				Field bottomLeft = Grid[w.X, w.Y];
				Field bottomRight = Grid[w.X + 1, w.Y];
				topLeft.Neighbors.Remove(bottomLeft);
				bottomLeft.Neighbors.Remove(topLeft);
				topRight.Neighbors.Remove(bottomRight);
				bottomRight.Neighbors.Remove(topRight);
			}
			else
			{
				Field topLeft = Grid[w.X - 1, w.Y];
				Field topRight = Grid[w.X, w.Y];
				Field bottomLeft = Grid[w.X - 1, w.Y + 1];
				Field bottomRight = Grid[w.X, w.Y + 1];
				topLeft.Neighbors.Remove(topRight);
				topRight.Neighbors.Remove(topLeft);
				bottomLeft.Neighbors.Remove(bottomRight);
				bottomRight.Neighbors.Remove(bottomLeft);
			}
			if (activePlayers.Any(p => !CanReachTarget(p)))
			{
				RemoveWall(w);
				return false;
			}
			wallList.Add(w);
			return true;
		}

		private bool CanReachTarget(Player p)
		{
			bool[,] visited = new bool[SIZE, SIZE];
			visited[p.X, p.Y] = true;
			Queue<Field> front = new Queue<Field>();
			front.Enqueue(Grid[p.X, p.Y]);
			while (front.Count > 0)
			{
				Field f = front.Dequeue();
				foreach (Field f2 in f.Neighbors)
				{
					if (!visited[f2.X, f2.Y])
					{
						visited[f2.X, f2.Y] = true;
						if (p.ID == 0 && f2.X == SIZE - 1 || p.ID == 1 && f2.X == 0 || p.ID == 2 && f2.Y == SIZE - 1)
						{
							return true;
						}
						front.Enqueue(f2);
					}
				}
			}
			return false;
		}

		public void RemoveWall(Wall w)
		{
			Walls[w.X, w.Y, w.Horizontal ? hor : ver] = false;
			if (w.Horizontal)
			{
				Field topLeft = Grid[w.X, w.Y - 1];
				Field topRight = Grid[w.X + 1, w.Y - 1];
				Field bottomLeft = Grid[w.X, w.Y];
				Field bottomRight = Grid[w.X + 1, w.Y];
				topLeft.Neighbors.Add(bottomLeft);
				bottomLeft.Neighbors.Add(topLeft);
				topRight.Neighbors.Add(bottomRight);
				bottomRight.Neighbors.Add(topRight);
			}
			else
			{
				Field topLeft = Grid[w.X - 1, w.Y];
				Field topRight = Grid[w.X, w.Y];
				Field bottomLeft = Grid[w.X - 1, w.Y + 1];
				Field bottomRight = Grid[w.X, w.Y + 1];
				topLeft.Neighbors.Add(topRight);
				topRight.Neighbors.Add(topLeft);
				bottomLeft.Neighbors.Add(bottomRight);
				bottomRight.Neighbors.Add(bottomLeft);
			}
		}

		public void Tick(string path)
		{
			foreach (Player p in activePlayers.ToList())
			{
				Console.WriteLine("###Input " + p.ID);
				if (Round == 1)
				{
					Console.WriteLine($"{SIZE} {SIZE} {playerCount} {p.ID}");
				}
				for (int i = 0; i < playerCount; i++)
				{
					Player toPrint = activePlayers.FirstOrDefault(pl => pl.ID == i);
					if (toPrint == null) Console.WriteLine("-1 -1 -1");
					else Console.WriteLine(toPrint);
				}
				Console.WriteLine(wallList.Count);
				foreach (Wall w in wallList) Console.WriteLine(w);
				Console.WriteLine("###Output " + p.ID + " 1");

				string[] action = Console.ReadLine().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
				if (directions.Contains(action[0].ToUpper())) //movement
				{
					int index = directions.ToList().IndexOf(action[0].ToUpper());
					int x = p.X + offset[index, 0];
					int y = p.Y + offset[index, 1];
					if (Grid[p.X, p.Y].Neighbors.Any(n => n.X == x && n.Y == y))
					{
						p.X = x;
						p.Y = y;
						if (p.ID == 0 && p.X == SIZE - 1 || p.ID == 1 && p.X == 0 || p.ID == 2 && p.Y == SIZE - 1)
						{
							activePlayers.Remove(p);
							finishedPlayers.Add(p);
						}
					}
					else
					{
						activePlayers.Remove(p);
						Console.Error.WriteLine($"Player {p.ID}: illegal movement");
					}
				}
				else
				{
					try
					{
						int x = int.Parse(action[0]);
						int y = int.Parse(action[1]);
						bool hor = action[2].ToUpper() == "H";
						Wall wall = new Wall(x, y, hor, p.ID);
						p.RemainingWalls--;

						if (!"HV".Contains(action[2].ToUpper()) || p.RemainingWalls < 0 || !AddWall(wall))
						{
							activePlayers.Remove(p);
							Console.Error.WriteLine($"Player {p.ID}: invalid wall placement");
							continue;
						}
					}
					catch
					{
						activePlayers.Remove(p);
						Console.Error.WriteLine($"Player {p.ID}: invalid input");
					}
				}


				if (path != null)
				{
					Bitmap bmp = Draw();
					bmp.Save(path + System.IO.Path.DirectorySeparatorChar + $"{++frame:000}.png");
					bmp.Dispose();
				}
			}
		}

		private int frame = 0;
		public Bitmap Draw()
		{
			Color[] colors = new Color[] { Color.Orange, Color.Red, Color.Blue };
			int scale = 50;
			Bitmap bmp = new Bitmap(SIZE * scale, SIZE * scale);
			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.Clear(Color.White);
				for (int i = 1; i < SIZE; i++)
				{
					g.DrawLine(Pens.Black, 0, i * scale, SIZE * scale, i * scale);
					g.DrawLine(Pens.Black, i * scale, 0, i * scale, SIZE * scale);
				}
				foreach (Player p in activePlayers)
				{
					g.FillEllipse(new SolidBrush(colors[p.ID]), p.X * scale, p.Y * scale, scale, scale);
				}
				foreach (Wall w in wallList)
				{
					if (w.Horizontal) g.FillRectangle(new SolidBrush(colors[w.Owner]), w.X * scale, w.Y * scale - 5, 2 * scale, 10);
					else g.FillRectangle(new SolidBrush(colors[w.Owner]), w.X * scale - 5, w.Y * scale, 10, 2 * scale);
				}
			}
			return bmp;
		}

		public bool Play()
		{
			return Round++ < MAX_ROUNDS && activePlayers.Count > 1;
		}

		public void DeclareWinner()
		{
			string result = "###End ";
			//winners
			if (finishedPlayers.Count > 0) result += string.Join(" ", finishedPlayers.Select(f => f.ID)) + " ";
			//moving players
			if (activePlayers.Count > 0) result += string.Concat(activePlayers.Select(f => f.ID)) + " ";
			//crashed players
			result += string.Concat(Enumerable.Range(0, playerCount).Where(i => !finishedPlayers.Union(activePlayers).Any(f => f.ID == i)).Select(i => i.ToString()));

			Console.WriteLine(result.Trim());
		}
	}

	class Wall
	{
		public int X { get; private set; }
		public int Y { get; private set; }
		public bool Horizontal { get; private set; }
		public int Owner { get; private set; }

		public Wall(int x, int y, bool horizontal, int owner)
		{
			this.X = x;
			this.Y = y;
			this.Horizontal = horizontal;
			this.Owner = owner;
		}

		public override string ToString()
		{
			string orientation = Horizontal ? "H" : "V";
			return $"{X} {Y} {orientation}";
		}
	}

	class Field
	{
		public int X { get; private set; }
		public int Y { get; private set; }
		public HashSet<Field> Neighbors { get; private set; }

		public Field(int x, int y)
		{
			this.X = x;
			this.Y = y;
			Neighbors = new HashSet<Field>();
		}

		public override string ToString()
		{
			return $"[Field: X={X}, Y={Y}, Neighbors={Neighbors.Count}]";
		}
	}

	class Player
	{
		public int ID { get; private set; }
		public int X { get; set; }
		public int Y { get; set; }
		public int RemainingWalls { get; set; }

		public Player(int id, Random random, int playerCount)
		{
			this.ID = id;
			switch (id)
			{
			case 0: X = 0; Y = random.Next(Board.SIZE); break;
			case 1: X = Board.SIZE - 1; Y = random.Next(Board.SIZE); break;
			case 2: X = random.Next(Board.SIZE); Y = 0; break;
			}
			this.RemainingWalls = playerCount == 2 ? 10 : 6;
		}

		public override string ToString()
		{
			return $"{X} {Y} {RemainingWalls}";
		}
	}
}