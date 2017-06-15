using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

class BackToTheCodeReferee
{
	public static void Main(string[] args)
	{
		string path = args.Length == 1 ? args[0] : null;
		string line = Console.ReadLine();
		int playerCount = int.Parse(line.Split()[1]);
		Board board = new Board(playerCount);
		int tick = 0;
		while (board.Tick ()) {
			tick++;
			if (path != null)
			{
				Bitmap bmp = board.Draw();
				bmp.Save(path + System.IO.Path.DirectorySeparatorChar + $"{tick:000}.png");
				bmp.Dispose();
			}
		}
		Console.WriteLine (board.Ranking ());
	}
}

class Board
{
	public static readonly int MAX_ROUNDS = 200;
	public static readonly int WIDTH = 35;
	public static readonly int HEIGHT = 20;
	private List<Player> players = new List<Player>();
	private Field[,] grid = new Field[WIDTH, HEIGHT];
	List<Field> allFields = new List<Field>();
	private int round = 0;

	public Board(int playerCount)
	{
		Random random = new Random();
		for (int i = 0; i < playerCount; i++) {
			players.Add(new Player(i, random.Next(WIDTH), random.Next(HEIGHT)));
		}
		for (int x = 0; x < WIDTH; x++) {
			for (int y = 0; y < HEIGHT; y++) {
				grid[x, y] = new Field(x, y);
				allFields.Add (grid [x, y]);
			}
		}
	}

	public Bitmap Draw() {
		Color[] colors = new Color[] { Color.Gray, Color.Orange, Color.Red, Color.Aquamarine, Color.Purple };
		Bitmap bmp = new Bitmap (20 * WIDTH, 20 * HEIGHT + 50);
		using (Graphics g = Graphics.FromImage (bmp)) {
			g.Clear (Color.Black);
			for (int x = 0; x < WIDTH; x++) {
				for (int y = 0; y < HEIGHT; y++) {
					using (Brush b = new SolidBrush (colors [grid [x, y].Owner + 1]))
						g.FillRectangle (b, 20 * x + 1, 20 * y + 1, 18, 18);
				}
			}
			g.FillRectangle (Brushes.Gray, 0, bmp.Height - 50, bmp.Width, 50);
			for (int i = 0; i < players.Count; i++) {
				using (Brush b = new SolidBrush (colors [i + 1])) {
					g.DrawString (allFields.Count (f => f.Owner == i).ToString (),
						new Font (new FontFamily ("Arial"), 30),
						b, bmp.Width / 2 + 200 * (i - players.Count / 2f), 20 * HEIGHT + 5);
					g.FillEllipse (b, 20 * players [i].X + 1, 20 * players [i].Y + 1, 18, 18);
				}
				g.DrawEllipse (Pens.Black, 20 * players [i].X + 1, 20 * players [i].Y + 1, 18, 18);
			}
		}
		return bmp;
	}

	public bool Tick()
	{
		if (++round > MAX_ROUNDS || players.Count (p => !p.Dead) < 2) {
			return false;
		}
		string input = this.ToString();

		foreach (Player p in players.Where(pl => !pl.Dead)) {
			string tmp = round + "\n" + p.ToString() + "\n";
			foreach (Player q in players.Where(pl => pl != p))
				tmp += q.ToString () + "\n";
			if (round == 1)
				tmp = (players.Count - 1) + "\n" + tmp;
			tmp += input.Replace("0", "x").Replace(p.ID.ToString(), "0").Replace("x", p.ID.ToString());
			p.GetAction(tmp);
		}
		int back = players.Where(p => !p.Dead).Sum(p => p.TimeBackRound);
		if (back > 0) {
			round -= back;
			if (round < 0)
				round = 0;
			foreach (Player p in players.Where(p => !p.Dead))
				p.Timeback(round);
			for (int x = 0; x < WIDTH; x++) {
				for (int y = 0; y < HEIGHT; y++) {
					grid[x, y].Timeback(round);
				}
			}
		}
		foreach (Player p in players.Where(p => !p.Dead)) {
			if (players.Any(q => p != q && p.X == q.X && p.Y == q.Y))
				continue;
			if (grid[p.X, p.Y].Owner == -1)
				grid[p.X, p.Y].Conquer(p.ID, round);
		}
		bool[,] visited = new bool[WIDTH, HEIGHT];
		for (int x = 0; x < WIDTH; x++) {
			for (int y = 0; y < HEIGHT; y++) {
				FillComponent(x, y, visited);
			}
		}
		return true;
	}

	public string Ranking()
	{
		int[] ids = players.Select(p => p.ID).ToArray();
		int[] fields = ids.Select (id => allFields.Count (f => f.Owner == id)).ToArray ();
		Array.Sort(fields, ids);
		string result = "###End " + ids[ids.Length - 1];
		for (int i = ids.Length - 2; i >= 0; i--) {
			if (fields[i] < fields[i + 1])
				result += " ";
			result += ids[i];
		}
		return result;
	}

	private static int[,] offset = new int[,] {
		{ -1, -1 },
		{ -1, 0 },
		{ -1, 1 },
		{ 0, -1 },
		{ 0, 1 },
		{ 1, -1 },
		{ 1, 0 },
		{ 1, 1 }
	};
	private void FillComponent(int x, int y, bool[,] visited)
	{
		if (visited[x, y] || grid[x, y].Owner != -1)
			return;
		List<Point> points = new List<Point>();
		Stack<Point> stack = new Stack<Point>();
		stack.Push(new Point(x, y));
		while (stack.Count > 0) {
			Point p = stack.Pop();
			points.Add(p);
			visited[p.X, p.Y] = true;
			for (int dir = 0; dir < 8; dir++) {
				Point q = new Point(p.X + offset[dir, 0], p.Y + offset[dir, 1]);
				if (q.X >= 0 && q.X < WIDTH && q.Y >= 0 && q.Y < HEIGHT && !visited[q.X, q.Y] && grid[q.X, q.Y].Owner == -1)
					stack.Push(q);
			}
		}
		bool[] neighbors = new bool[players.Count];
		bool edge = false;
		foreach (Point p in points) {
			for (int dir = 0; dir < 8; dir++) {
				Point q = new Point(p.X + offset[dir, 0], p.Y + offset[dir, 1]);
				if (q.X < 0 || q.X >= WIDTH || q.Y < 0 || q.Y >= HEIGHT)
					edge = true;
				else if (grid[q.X, q.Y].Owner != -1)
					neighbors[grid[q.X, q.Y].Owner] = true;
			}
		}
		if (!edge && neighbors.Count(n => n) == 1) {
			int owner = Enumerable.Range(0, players.Count).First(o => neighbors[o]);
			foreach (Point p in points)
				grid[p.X, p.Y].Conquer(owner, round);
		}
	}

	public override string ToString()
	{
		StringBuilder result = new StringBuilder();
		for (int y = 0; y < HEIGHT; y++) {
			for (int x = 0; x < WIDTH; x++) {
				result.Append(grid[x, y]);
			}
			result.AppendLine();
		}
		return result.ToString();
	}

}

class Field
{
	public int X { get; private set; }
	public int Y { get; private set; }
	public int Owner { get; private set; }
	public int ConqueredRound { get; private set; }

	public Field(int x, int y)
	{
		this.X = x;
		this.Y = y;
		this.Owner = -1;
	}

	public void Timeback(int newRound)
	{
		if (ConqueredRound > newRound)
			Owner = -1;
	}

	public void Conquer(int owner, int round)
	{
		this.Owner = owner;
		this.ConqueredRound = round;
	}

	public override string ToString()
	{
		return Owner == -1 ? "." : Owner.ToString();
	}
}

class Player
{
	public static readonly int MAX_TIMEBACK = 25;
	public int ID { get; private set; }
	public int X { get; private set; }
	public int Y { get; private set; }
	public bool Dead { get; private set; }
	public int TimeBack { get; private set; }
	public int TimeBackRound { get; private set; }

	private List<Point> positions = new List<Point>();
	public Player(int id, int x, int y)
	{
		this.ID = id;
		this.X = x;
		this.Y = y;
		this.TimeBack = 1;
		positions.Add(new Point(x, y));
	}

	public void Timeback(int round)
	{
		positions = Enumerable.Range(0, round + 1).Select(i => positions[i]).ToList();
		this.X = positions.Last().X;
		this.Y = positions.Last().Y;
	}

	public void GetAction(string input)
	{
		TimeBackRound = 0;
		Console.WriteLine("###Input " + ID);
		Console.Write(input);
		Console.WriteLine("###Output " + ID + " 1");
		string action = Console.ReadLine();
		string[] parts = action.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
		int tmp;
		if (parts.Length < 2 || parts [0].ToUpper () != "BACK" && !int.TryParse (parts [0], out tmp) || !int.TryParse (parts [1], out tmp)) {
			Console.Error.WriteLine ("###Error " + ID + " Lost invalid input");
			Dead = true;
			return;
		}
		int y = int.Parse(parts[1]);
		if (parts[0].ToUpper() == "BACK") {
			if (this.TimeBack > 0) {
				this.TimeBack--;
				if (y > MAX_TIMEBACK) {
					Console.Error.WriteLine("###Error " + ID + " Lost can't go that far back");
					Dead = true;
				} else
					TimeBackRound = y;
			} else {
				Console.Error.WriteLine("###Error " + ID + " Lost no more timeback");
				Dead = true;
			}
		} else {
			int x = int.Parse(parts[0]);
			if (this.X < x)
				this.X++;
			else if (this.X > x)
				this.X--;
			else if (this.Y < y)
				this.Y++;
			else if (this.Y > y)
				this.Y--;
			positions.Add(new Point(this.X, this.Y));
		}
	}

	public override string ToString()
	{
		if (Dead)
			return "-1 -1 0";
		return this.X + " " + this.Y + " " + this.TimeBack;
	}
}