﻿﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public class SmashTheCodeReferee
{
	static readonly int MAX_ROUNDS = 200;
	static readonly int PREVIEW = 8;
	public static void Main(string[] args)
	{
		string path = args.Length == 1 ? args[0] : null;
		Random random = new Random();
		Stone[] allStones = new Stone[MAX_ROUNDS + PREVIEW];
		for (int i = 0; i < allStones.Length; i++)
		{
			allStones[i] = new Stone(random);
		}

		//read ###Start 2, as there are 2 players
		Console.ReadLine();

		Player p1 = new Player(0);
		Player p2 = new Player(1);
		for (int round = 0; round < MAX_ROUNDS; round++)
		{
			Stone[] visibleStones = Enumerable.Range(round, PREVIEW).Select(i => allStones[i]).ToArray();
			if (path != null)
			{
				Bitmap bmp = new Bitmap((Board.WIDTH * 2 + 1) * 20, Board.HEIGHT * 20 + 80);
				using (Graphics g = Graphics.FromImage(bmp))
				{
					g.Clear(Color.White);
					for (int i = 0; i < visibleStones.Length; i++)
					{
						using (Brush b = new SolidBrush(Stone.Colors[visibleStones[i].Color1]))
							g.FillEllipse(b, bmp.Width / 2 - 42 + 12 * i, 55, 10, 10);
						using (Brush b = new SolidBrush(Stone.Colors[visibleStones[i].Color2]))
							g.FillEllipse(b, bmp.Width / 2 - 42 + 12 * i, 65, 10, 10);
					}
				}
				p1.Draw(bmp);
				p2.Draw(bmp);
				bmp.Save(path + System.IO.Path.DirectorySeparatorChar + $"{round:000}.png");
				bmp.Dispose();
			}

			List<string> p1Input = p1.GiveInput(p2, visibleStones);
			List<string> p2Input = p2.GiveInput(p1, visibleStones);
			Console.WriteLine(string.Join("\n", p1Input));
			int p1Skulls = p1.ReadAction(allStones[round]);
			bool p1Alive = p1Skulls >= 0;
			Console.WriteLine(string.Join("\n", p2Input));
			int p2Skulls = p2.ReadAction(allStones[round]);
			bool p2Alive = p2Skulls >= 0;
			p1.AddSkulls(p2Skulls);
			p2.AddSkulls(p1Skulls);
			if (!p1Alive || !p2Alive) break;
		}
		//win by score
		if (p1.Score > p2.Score)
			Console.WriteLine("###End 0 1");
		else if (p1.Score < p2.Score)
			Console.WriteLine("###End 1 0");
		else
			Console.WriteLine("###End 01");
	}

	public class Player
	{
		public int ID { get; private set; }
		public int Score { get; private set; }
		private Board board = new Board();

		public Player(int id)
		{
			this.ID = id;
		}

		public void Draw(Bitmap bmp)
		{
			board.Draw(bmp, this);
		}

		public List<String> GiveInput(Player opponent, Stone[] stones)
		{
			List<string> result = new List<string>();
			result.Add("###Input " + ID);
			foreach (Stone s in stones)
			{
				result.Add(s.Color1 + " " + s.Color2);
			}
			result.Add(this.Score.ToString());
			result.AddRange(this.board.Print());
			result.Add(opponent.Score.ToString());
			result.AddRange(opponent.board.Print());
			return result;
		}

		public void AddSkulls(int rows)
		{
			this.board.AddSkulls(rows);
		}

		/// <summary>
		/// read action from player and apply it
		/// </summary>
		/// <param name="stone">the stone to place next</param>
		/// <returns>the number of skull rows or -1 for an invalid action</returns>
		public int ReadAction(Stone stone)
		{
			Console.WriteLine("###Output " + ID + " 1");
			string line = Console.ReadLine();
			try
			{
				string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
				int x = int.Parse(parts[0]);
				int rot = int.Parse(parts[1]);
				if (x < 0 || x >= Board.WIDTH || rot < 0 || rot > 3)
					return -1; //out of bounds
				int roundScore = board.PlaceStone(stone, x, rot);
				if (roundScore < 0)
				{
					this.Score = -1;
					return -1;
				}
				int oldSkullRows = Score / (Board.WIDTH * Board.SKULL_COST);
				Score += roundScore;
				int newSkullRows = Score / (Board.WIDTH * Board.SKULL_COST);
				return newSkullRows - oldSkullRows;
			}
			catch (Exception)
			{ //invalid format
				this.Score = -1;
				return -1;
			}
		}
	}

	public class Board
	{
		public static readonly int WIDTH = 6;
		public static readonly int HEIGHT = 12;
		public static readonly int SKULL_COST = 70;
		private static readonly int FREE = -1;
		private static readonly int SKULL = 0;
		private static readonly int GROUP_MIN_SIZE = 4;

		private int[,] grid = new int[WIDTH, HEIGHT];

		public Board()
		{
			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					grid[x, y] = FREE;
				}
			}
		}

		public void Draw(Bitmap bmp, Player p)
		{
			using (Graphics g = Graphics.FromImage(bmp))
			{
				for (int x = 0; x < WIDTH; x++)
				{
					for (int y = 0; y < HEIGHT; y++)
					{
						if (grid[x, y] == FREE) break;
						using (Brush b = new SolidBrush(Stone.Colors[grid[x, y]]))
							g.FillEllipse(b, 20 * (x + 7 * p.ID), bmp.Height - 20 * (1 + y), 20, 20);
					}
				}
				g.DrawString(p.Score.ToString(), new Font(new FontFamily("Arial"), 30), Brushes.Black, 5 + 140 * p.ID, 5);
			}
		}

		/// <summary>
		/// place a stone in the board
		/// </summary>
		/// <param name="stone">the stone</param>
		/// <param name="x">the location</param>
		/// <param name="rotation">the rotation</param>
		/// <returns>the score for placing the stone or -1 if the move is invalid</returns>
		public int PlaceStone(Stone stone, int x, int rotation)
		{
			if (grid[x, HEIGHT - 1] != FREE ||
				rotation % 2 == 1 && grid[x, HEIGHT - 2] != FREE ||
				rotation == 0 && (x == WIDTH - 1 || grid[x + 1, HEIGHT - 1] != FREE) ||
				rotation == 2 && (x == 0 || grid[x - 1, HEIGHT - 1] != FREE))
				return -1;

			//place stone
			switch (rotation)
			{
			case 0:
				grid[x, HEIGHT - 1] = stone.Color1;
				grid[x + 1, HEIGHT - 1] = stone.Color2;
				break;
			case 1:
				grid[x, HEIGHT - 2] = stone.Color1;
				grid[x, HEIGHT - 1] = stone.Color2;
				break;
			case 2:
				grid[x, HEIGHT - 1] = stone.Color1;
				grid[x - 1, HEIGHT - 1] = stone.Color2;
				break;
			case 3:
				grid[x, HEIGHT - 1] = stone.Color1;
				grid[x, HEIGHT - 2] = stone.Color2;
				break;
			}
			return Eval(grid);
		}

		public void AddSkulls(int rows)
		{
			for (int x = 0; x < WIDTH; x++)
			{
				int y = 0;
				while (y + 1 < HEIGHT && grid[x, y] != FREE)
					y++;
				for (int r = 0; r < rows && r + y < HEIGHT; r++)
					grid[x, r + y] = SKULL;
			}
		}

		private static readonly int[] cb = new int[] { 0, 0, 2, 4, 8, 16 };
		private static int CB(bool[] colors)
		{
			return cb[colors.Count(c => c)];
		}

		private static readonly int[] gb = new int[] { 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6 };
		private static int GB(int n)
		{
			if (n >= gb.Length)
				return 8;
			return gb[n];
		}

		private static int Eval(int[,] grid)
		{
			int result = 0;
			int chain = 0;
			while (true)
			{
				BlockFalling(grid);
				int removedStones = 0;
				int score = BlockRemove(grid, out removedStones) + chain;
				if (removedStones == 0)
					break;
				if (score == 0)
					score = 1;
				result += 10 * removedStones * score;
				if (chain == 0)
					chain = 4;
				chain *= 2;
			}
			return result;
		}

		private static readonly int[,] offset = new int[,] { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 } };
		private static int BlockRemove(int[,] grid, out int stonesRemoved)
		{
			List<List<Point>> blocks = new List<List<Point>>();
			bool[,] visited = new bool[WIDTH, HEIGHT];
			//find groups
			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					if (visited[x, y] || grid[x, y] == FREE || grid[x, y] == SKULL)
						continue;
					Queue<Point> active = new Queue<Point>();
					active.Enqueue(new Point(x, y));
					List<Point> found = new List<Point>();
					while (active.Count > 0)
					{
						Point p = active.Dequeue();
						if (visited[p.X, p.Y])
							continue;
						visited[p.X, p.Y] = true;
						found.Add(p);
						for (int dir = 0; dir < 4; dir++)
						{
							Point q = new Point(p.X + offset[dir, 0], p.Y + offset[dir, 1]);
							if (q.X >= 0 && q.Y >= 0 && q.X < WIDTH && q.Y < HEIGHT && grid[p.X, p.Y] == grid[q.X, q.Y])
								active.Enqueue(q);
						}
					}
					if (found.Count >= GROUP_MIN_SIZE)
						blocks.Add(found);
				}
			}

			//remove groups
			bool[] colorsUsed = new bool[cb.Length];
			foreach (List<Point> block in blocks)
			{
				foreach (Point p in block)
				{
					colorsUsed[grid[p.X, p.Y]] = true;
					grid[p.X, p.Y] = FREE;
					for (int dir = 0; dir < 4; dir++)
					{
						Point q = new Point(p.X + offset[dir, 0], p.Y + offset[dir, 1]);
						if (q.X >= 0 && q.Y >= 0 && q.X < WIDTH && q.Y < HEIGHT && grid[q.X, q.Y] == SKULL)
							grid[q.X, q.Y] = FREE;
					}
				}
			}

			//calculate score
			stonesRemoved = blocks.Sum(block => block.Count);
			return CB(colorsUsed) + blocks.Sum(block => GB(block.Count));
		}

		private static void BlockFalling(int[,] grid)
		{
			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 1; y < HEIGHT; y++)
				{
					if (grid[x, y - 1] == FREE && grid[x, y] != FREE)
					{
						int destY = y - 1;
						while (destY > 0 && grid[x, destY - 1] == FREE)
						{
							destY--;
						}
						grid[x, destY] = grid[x, y];
						grid[x, y] = FREE;
					}
				}
			}
		}

		public List<string> Print()
		{
			string[] lines = new string[HEIGHT];
			for (int y = 0; y < HEIGHT; y++)
			{
				string line = "";
				for (int x = 0; x < WIDTH; x++)
				{
					line += grid[x, y] == FREE ? "." : grid[x, y].ToString();
				}
				lines[HEIGHT - 1 - y] = line;
			}
			return lines.ToList();
		}

	}

	public class Stone
	{
		public static Color[] Colors = new Color[] { Color.Gray, Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Purple };
		public int Color1 { get; private set; }
		public int Color2 { get; private set; }
		private static readonly int COLOR_COUNT = 5;

		public Stone(Random random)
		{
			Color1 = random.Next(COLOR_COUNT) + 1;
			Color2 = random.Next(COLOR_COUNT) + 1;
		}
	}
}