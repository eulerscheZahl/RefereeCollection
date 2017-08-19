using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

class TronReferee
{
	public static void Main(string[] args)
	{
		string path = args.Length == 1 ? args[0] : null;

		int seed = -1;
		while (true) {
			string[] lineParts = Console.ReadLine ().Split ();
			if (lineParts [0] == "###Seed")
				seed = int.Parse (lineParts [1]);
			else if (lineParts [0] == "###Start") {
				int playerCount = int.Parse (lineParts [1]);
				Board board = new Board (playerCount, seed);
				if (path != null) {
					Bitmap bmp = board.Draw ();
					bmp.Save (path + System.IO.Path.DirectorySeparatorChar + "000.png");
					bmp.Dispose ();
				}

				while (board.Play ()) {
					board.Tick (path);
				}
				board.DeclareWinner ();
			}
		}
	}

	class Board
	{
		public static readonly int WIDTH = 30;
		public static readonly int HEIGHT = 20;

		private static Random random = new Random();
		private List<Player> activePlayers = new List<Player>();
		private List<Player> deadPlayers = new List<Player>();
		private int playerCount;
		public int[,] Grid = new int[WIDTH, HEIGHT];

		public Board(int playerCount, int seed)
		{
			if (seed >= 0) random = new Random(seed);
			this.playerCount = playerCount;
			for (int i = 0; i < playerCount; i++)
			{
				activePlayers.Add(new Player(i, random, this));
			}            
		}

		private int round = 0;
		public void Tick(string path)
		{
			foreach (Player p in activePlayers.ToList()) {
				Console.WriteLine ("###Input " + p.ID);
				Console.WriteLine ($"{playerCount} {p.ID}");
				for (int i = 0; i < playerCount; i++) {
					Player toPrint = activePlayers.FirstOrDefault (pl => pl.ID == i);
					if (toPrint == null)
						Console.WriteLine ("-1 -1 -1 -1");
					else
						Console.WriteLine (toPrint);
				}
				Console.WriteLine ("###Output " + p.ID + " 1");

				string action = Console.ReadLine ().Split (" ".ToCharArray (), StringSplitOptions.RemoveEmptyEntries) [0];
				if (!p.Move (action, this)) {
					activePlayers.Remove (p);
					deadPlayers.Add (p);
					for (int x = 0; x < WIDTH; x++) {
						for (int y = 0; y < HEIGHT; y++) {
							if (Grid [x, y] == p.ID + 1)
								Grid [x, y] = 0;
						}
					}
				}

				if (path != null) {
					Bitmap bmp = Draw ();
					bmp.Save (path + System.IO.Path.DirectorySeparatorChar +$"{++frame:000}.png");
					bmp.Dispose ();
				}
			}
			round++;
		}

		private int frame = 0;
		public Bitmap Draw()
		{
			Color[] colors = new Color[] {
				Color.Gray,
				Color.Orange,
				Color.Red,
				Color.Blue,
				Color.Violet
			};
			int scale = 30;
			Bitmap bmp = new Bitmap (WIDTH * scale, HEIGHT * scale);
			using (Graphics g = Graphics.FromImage (bmp)) {
				g.Clear (Color.Black);
				for (int x = 0; x < WIDTH; x++) {
					for (int y = 0; y < HEIGHT; y++) {
						g.FillRectangle (new SolidBrush (colors [Grid [x, y]]), x * scale + 1, y * scale + 1, scale - 2, scale - 2);
					}
				}
			}
			return bmp;
		}

		public bool Play()
		{
			return activePlayers.Count > 1;
		}

		public void DeclareWinner()
		{
			deadPlayers.AddRange (activePlayers);
			deadPlayers.Reverse ();
			Console.WriteLine ("###End " + string.Join (" ", deadPlayers.Select (d => d.ID)));
		}
	}

	class Player
	{
		public int ID { get; private set; }
		public int X { get; set; }
		public int Y { get; set; }
		public int PrevX { get; set; }
		public int PrevY { get; set; }

		public Player(int id, Random random, Board board)
		{
			this.ID = id;
			int x = -1, y = -1;
			do
			{
				x = random.Next(Board.WIDTH);
				y = random.Next(Board.HEIGHT);
			} while (board.Grid[x, y] != 0);
			board.Grid[x, y] = id + 1;
			this.X = x;
			this.Y = y;
			this.PrevX = x;
			this.PrevY = y;
		}

		private static int[,] offset = new int[,] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };
		private static string[] directions = new string[] { "DOWN", "RIGHT", "UP", "LEFT" };
		public bool Move(string dir, Board board)
		{
			int index = directions.ToList ().IndexOf (dir.ToUpper ());
			if (index == -1)
				return false;
			int x = this.X + offset [index, 0];
			int y = this.Y + offset [index, 1];
			if (x < 0 || x >= Board.WIDTH || y < 0 || y >= Board.HEIGHT || board.Grid [x, y] != 0)
				return false;
			board.Grid [x, y] = ID + 1;
			PrevX = this.X;
			this.X = x;
			PrevY = this.Y;
			this.Y = y;
			return true;
		}

		public override string ToString()
		{
			return $"{PrevX} {PrevY} {X} {Y}";
		}
	}
}