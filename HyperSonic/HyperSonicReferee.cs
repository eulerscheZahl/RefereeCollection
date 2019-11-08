using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;


class JavaRandom {
		public uint next(uint bits)
		{
			seed = (seed * 0x5DEECE66DLL + 0xBLL) & ((1LL << 48) - 1);
			return (uint)(seed >> (int)(48 - bits));
		}

		ulong seed;

		public static ulong CurrentTimeMillis()
		{
			DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return (ulong) (DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
		}		
		public void setSeed(ulong _seed)
		{
			seed = (_seed ^ 0x5DEECE66DLL) & ((1LL << 48) - 1);
		}
		public JavaRandom()
		{
			setSeed(CurrentTimeMillis());
		}
		public JavaRandom(ulong _seed)
		{
			setSeed(_seed);
		}
		public int nextInt()
		{
			return (int)next(32);
		}

		public	int nextInt(int n)
		{
			if ((n & -n) == n) // i.e., n is a power of 2
				return (int)(((ulong)n * (ulong)next(31)) >> 31);
			int bits, val;
			do
			{
				bits = (int)next(31);
				val = bits % n;
			} while (bits - val + (n - 1) < 0);
			return val;
		}
		public long nextLong()
		{
			return (long)(((ulong)next(32) << 32) + (ulong)next(32));
		}
		public bool nextBoolean()
		{
			return next(1) != 0;
		}

	};


class Referee
{
	public static int[,] Offset = new int[,] {
		{ 0, 1 },
		{ 0, -1 },
		{ 1, 0 },
		{ -1, 0 }
	};
	public static List<Player> Players;
	public static void Main(string[] args)
	{
		string path = args.Length == 1 ? args[0] : null;
		int players =2;		
		string start = Console.ReadLine ();
		JavaRandom random = new JavaRandom();
		ulong seed = (ulong)random.nextInt(999999999);
		try {
			if (start.StartsWith("###Seed")) {
				seed = ulong.Parse( start.Substring(8));
				Console.Error.WriteLine("Using seed:"+seed);
				random = new JavaRandom(seed);
				start = Console.ReadLine (); 
			}
			players = int.Parse (start.Split () [1]);
		} catch (Exception e) {
			Console.Error.WriteLine ("Invalid parameter '"+start+"': "+e);
			return;
		}
		
		Board board = new Board(random, players);
		Players = board.players;
		int round = 0;
		while (board.Play()) {
			Console.Error.WriteLine (path);
			board.Tick();
			round++;
			if (path != null)
			{
				Console.Error.WriteLine ("draw " + round);
				Bitmap bmp = board.Draw ();
				bmp.Save(path + System.IO.Path.DirectorySeparatorChar + $"{round:000}.png");
				bmp.Dispose();
			}
		}
		List<Player> ranking = board.players.OrderByDescending(p => p.Score).ToList();
		string result = "###End " + ranking[0].ID;
		for (int i = 1; i < ranking.Count; i++) {
			if (ranking[i - 1].Score > ranking[i].Score)
				result += " ";
			result += ranking[i].ID;
		}
		Console.WriteLine(result);
	}
}

class Player
{
	public int ID { get; private set; }
	public int Score { get; set; }
	public Field Location { get; set; }
	public Field NewLocation { get; private set; }
	public bool PlaceBomb { get; private set; }
	public bool Dead { get; set; }
	public int BombFree { get; set; }
	public int BombRange { get; set; }

	public Player(int id, Board board)
	{
		this.ID = id;
		this.BombFree = 1;
		this.BombRange = 3;
		switch (id) {
		case 0:
			Location = board.Grid[0, 0];
			break;
		case 1:
			Location = board.Grid[Board.WIDTH - 1, Board.HEIGHT - 1];
			break;
		case 2:
			Location = board.Grid[Board.WIDTH - 1, 0];
			break;
		case 3:
			Location = board.Grid[0, Board.HEIGHT - 1];
			break;
		}
	}

	public void GetAction(string input)
	{
		Console.WriteLine("###Output " + ID + " 1");
		string[] line = Console.ReadLine().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
		int tmp;
		if (line.Length < 3 || line[0].ToUpper() != "MOVE" && line[0].ToUpper() != "BOMB" || !int.TryParse(line[1], out tmp) || !int.TryParse(line[2], out tmp)) {
			Console.Error.WriteLine ("###Error " + ID + " Lost invalid command: '" + string.Join (" ", line) + "'");
			Dead = true;
			return;
		}

		PlaceBomb = line[0].ToUpper() == "BOMB" && this.BombFree > 0 && Location.Bombs.Count == 0;
		int x = int.Parse(line[1]);
		int y = int.Parse(line[2]);
		if (x < 0 || x >= Board.WIDTH || y < 0 || y >= Board.HEIGHT) {
			Console.Error.WriteLine ("###Error " + ID + " Lost invalid target: " + x + "/" + y);
			Dead = true;
			return;
		}

		int distToTarget = Math.Abs(x - Location.X) + Math.Abs(y - Location.Y);
		NewLocation = Location;
		for (int dir = 0; dir < 4; dir++) {
			Field test = Location.Neighbors[dir];
			if (test != null && Math.Abs(x - test.X) + Math.Abs(y - test.Y) < distToTarget && !test.Box && !test.Wall && test.Bombs.Count == 0) {
				NewLocation = test;
				break;
			}
		}
	}

	public override string ToString()
	{
		return "0 " + ID + " " + Location.X + " " + Location.Y + " " + BombFree + " " + BombRange;
	}
}

class Board
{
	public static readonly int MAX_ROUNDS = 200;
	public static readonly int ROUNDS_AFTER_LAST_BOX = 20;
	public static readonly int WIDTH = 13;
	public static readonly int HEIGHT = 11;
	public static readonly int MIN_BOXES = 30;
	public static readonly int MAX_BOXES = 65;
	public Field[,] Grid { get; private set; }
	public List<Player> players = new List<Player>();
	private List<Field> allFields = new List<Field>();

	public bool Play()
	{
		return players.Count(p => !p.Dead) > 1 && round < MAX_ROUNDS;
	}

	public Board(JavaRandom random, int playerCount)
	{
		Grid = new Field[WIDTH, HEIGHT];
		for (int x = 0; x < WIDTH; x++) {
			for (int y = 0; y < HEIGHT; y++) {
				Grid[x, y] = new Field(x, y);
				allFields.Add(Grid[x, y]);
			}
		}
		foreach (Field f in allFields) {
			f.InitNeighbors(Grid);
		}
		for (int i = 0; i < playerCount; i++) {
			players.Add(new Player(i, this));
		}

		int boxCount = 30 + random.nextInt(36);
		int[] toDistribute = new int[2]{boxCount / 3,boxCount / 3};
		
		List<Field> startingPositions = new List<Field>(){Grid[0,0],Grid[0,HEIGHT-1],Grid[WIDTH-1,HEIGHT-1],Grid[WIDTH-1,0]};

		int iterations = 0;
		int boxId = 0;
		while (boxId < boxCount) {
			iterations++;

			if (iterations > 1000) {
				break;
			}

			int x = random.nextInt(WIDTH);
			int y = random.nextInt(HEIGHT);

			bool ok = true;
			foreach (var p in startingPositions) {
				if ((Math.Abs(p.X - x) + Math.Abs(p.Y - y)) < 2)
				{
					ok = false;
					break;
				}
			}
			if (!ok) continue;
			if (Grid[x, y].Wall || Grid[x, y].Box)
				continue;

			Grid[x, y].Box = true;
			for (int i=0;i<2;++i){
				if (toDistribute[i] >0)
				{
					Grid[x, y].PowerUp = new PowerUp { ExtraRange = (i==0),ExtraBomb = (i!=0), field = Grid[x, y] };
					--toDistribute[i];
					break;
				}
			}
			
			
			++boxId;
			List<Field> otherCells = new List<Field>(){ Grid[WIDTH - x - 1,y ],Grid[WIDTH - x - 1,HEIGHT - y - 1],Grid[x,HEIGHT - y - 1]};
			foreach (var c in otherCells){
				if (!c.Wall && !c.Box)
				{
					c.Box = true;
					++boxId;
					if (Grid[x, y].PowerUp != null)
					{
						c.PowerUp = new PowerUp { ExtraRange = Grid[x, y].PowerUp.ExtraRange,ExtraBomb = Grid[x, y].PowerUp.ExtraBomb, field = c };
						--toDistribute[c.PowerUp.ExtraRange?0:1];
					}
				}
			}
		}
	}

	public Bitmap Draw() {
		Bitmap bmp = (Bitmap)Bitmap.FromFile ("img" + Path.DirectorySeparatorChar + "background.png");
		using (Graphics g = Graphics.FromImage (bmp)) {
			for (int x = 0; x < WIDTH; x++) {
				for (int y = 0; y < HEIGHT; y++) {
					if (Grid [x, y].Wall)
						g.DrawImage (Bitmap.FromFile ("img" + Path.DirectorySeparatorChar + "Wall.png"), 700 + 90 * x, 43 + 90 * y);
					if (Grid[x,y].Box)
						g.DrawImage (Bitmap.FromFile ("img" + Path.DirectorySeparatorChar + "Box.png"), 700 + 90 * x, 43 + 90 * y);
					if (Grid[x,y].PowerUp != null && Grid[x,y].PowerUp.ExtraBomb)
						g.DrawImage (Bitmap.FromFile ("img" + Path.DirectorySeparatorChar + "bombExtra.png"), 712 + 90 * x, 55 + 90 * y);
					if (Grid[x,y].PowerUp != null && Grid[x,y].PowerUp.ExtraRange)
						g.DrawImage (Bitmap.FromFile ("img" + Path.DirectorySeparatorChar + "bombRange.png"), 712 + 90 * x, 55 + 90 * y);
					if (Grid [x, y].Bombs.Count > 0)
						g.DrawImage (Bitmap.FromFile ("img" + Path.DirectorySeparatorChar + "Bomb" + (Grid [x, y].Bombs.First ().Owner.ID + 1) + ".png"), 710 + 90 * x, 48 + 90 * y);
				}
			}
			foreach (Player p in players) {
				g.DrawImage (Bitmap.FromFile ("img" + Path.DirectorySeparatorChar + "Player" + (p.ID + 1) + ".png"), 705 + 90 * p.Location.X, 48 + 90 * p.Location.Y);
			}
		}
		return bmp;
	}

	private int round = 0;
	public void Tick()
	{
		round++;

		//ask players for next action
		string input = this.ToString();
		foreach (Player p in players.Where(p => !p.Dead)) {
			Console.WriteLine("###Input " + p.ID);
			if (round == 1)
				Console.WriteLine(WIDTH + " " + HEIGHT + " " + p.ID);
			Console.Write(input);
			p.GetAction(input);
		}

		//make bombs explode
		foreach (Field f in allFields) {
			f.BombDamage = new bool[4];
		}
		foreach (Bomb b in allFields.SelectMany(f => f.Bombs)) {
			b.Tick();
		}

		//give score for hit boxes, remove bombs stuff
		foreach (Field f in allFields) {
			for (int i = 0; i < f.BombDamage.Length; i++) {
				if (f.BombDamage[i] && f.Box)
					players[i].Score++;
			}
			if (f.BombDamage.Any(d => d)) {
				f.Bombs.Clear();
				if (f.Box)
					f.Box = false;
				else
					f.PowerUp = null;
				foreach (Player p in players.Where(p => !p.Dead && p.Location == f)) {
					Console.Error.WriteLine("###Error " + p.ID + " Lost hit by bomb :(");
					p.Dead = true;
				}
			}
		}

		//apply player actions
		foreach (Player p in players.Where(p => !p.Dead)) {
			p.Score += (1 + MAX_BOXES); //for staying alive

			if (p.PlaceBomb) {
				p.Location.Bombs.Add(new Bomb(p.Location, p.BombRange, p));
				p.BombFree--;
			}
			p.Location = p.NewLocation;
			if (p.Location.PowerUp != null) {
				if (p.Location.PowerUp.ExtraBomb)
					p.BombFree++;
				if (p.Location.PowerUp.ExtraRange)
					p.BombRange++;
			}
		}
		foreach (Player p in players) {
			p.Location.PowerUp = null;
		}

		bool earlyEnd = !allFields.Any(f => f.Box);
		if (earlyEnd && round + ROUNDS_AFTER_LAST_BOX < MAX_ROUNDS)
			round = MAX_ROUNDS - ROUNDS_AFTER_LAST_BOX;
	}

	private IEnumerable<Field> SymmetricFields(int x, int y)
	{
		if (x == WIDTH - 1 - x && y == HEIGHT - 1 - y || x + y < 2)
			yield break; //no box in center (already put before if odd number) and at player starting positions
		yield return Grid[x, y];
		if (x < WIDTH - 1 - x)
			yield return Grid[WIDTH - 1 - x, y];
		if (y < HEIGHT - 1 - y)
			yield return Grid[x, HEIGHT - 1 - y];
		if (x < WIDTH - 1 - x && y < HEIGHT - 1 - y)
			yield return Grid[WIDTH - 1 - x, HEIGHT - 1 - y];
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		for (int y = 0; y < HEIGHT; y++) {
			for (int x = 0; x < WIDTH; x++) {
				sb.Append(Grid[x, y]);
			}
			sb.AppendLine();
		}
		List<string> items = new List<string>();
		foreach (Player p in players.Where(p => !p.Dead))
			items.Add(p.ToString());
		foreach (Field f in allFields) {
			foreach (Bomb b in f.Bombs)
				items.Add(b.ToString());
			if (!f.Box && f.PowerUp != null)
				items.Add(f.PowerUp.ToString());
		}
		sb.AppendLine(items.Count.ToString());
		foreach (string item in items)
			sb.AppendLine(item);
		return sb.ToString();
	}
}

class Field
{
	public int X { get; private set; }
	public int Y { get; private set; }
	public bool Wall { get; private set; }
	public List<Bomb> Bombs { get; private set; }
	public bool Box { get; set; }
	public PowerUp PowerUp { get; set; }
	public Field[] Neighbors { get; private set; }
	public bool[] BombDamage { get; set; }

	public Field(int x, int y)
	{
		this.X = x;
		this.Y = y;
		this.Wall = x % 2 == 1 && y % 2 == 1;
		this.Bombs = new List<Bomb>();
	}

	public void InitNeighbors(Field[,] grid)
	{
		this.Neighbors = new Field[4];
		for (int dir = 0; dir < 4; dir++) {
			int x = this.X + Referee.Offset[dir, 0];
			int y = this.Y + Referee.Offset[dir, 1];
			if (x >= 0 & x < Board.WIDTH && y >= 0 && y < Board.HEIGHT)
				Neighbors[dir] = grid[x, y];
		}
	}

	public override string ToString()
	{
		if (Wall)
			return "X";
		if (Box) {
			if (PowerUp == null)
				return "0";
			if (PowerUp.ExtraRange)
				return "1";
			return "2";
		}
		return ".";
	}
}

class PowerUp
{
	public bool ExtraRange { get; set; }
	public bool ExtraBomb { get; set; }
	public Field field;

	public PowerUp()
	{
	}

	public override string ToString()
	{
		return "2 0 " + field.X + " " + field.Y + " " + (ExtraRange ? "1" : "2") + " " + (ExtraRange ? "1" : "2");
	}
}

class Bomb
{
	public static readonly int INITIAL_TIMER = 8;

	public int Timer { get; private set; }
	public Field Location { get; private set; }
	public int Range { get; private set; }
	public Player Owner { get; private set; }

	public Bomb(Field field, int range, Player owner)
	{
		this.Timer = INITIAL_TIMER;
		this.Location = field;
		this.Range = range;
		this.Owner = owner;
	}

	public void Tick()
	{
		if (--Timer == 0)
			Explode();
	}

	public void Explode()
	{
		Owner.BombFree++;
		Location.BombDamage[Owner.ID] = true;
		for (int dir = 0; dir < 4; dir++) {
			Field f = Location;
			for (int range = 1; range < this.Range; range++) {
				f = f.Neighbors[dir];
				if (f == null)
					break;
				f.BombDamage[Owner.ID] = true;
				if (f.Wall || f.Box || f.PowerUp != null)
					break;
				if (f.Bombs.Count > 0) {
					foreach (Bomb b in f.Bombs) {
						if (b.Timer > 0) {
							b.Timer = 0;
							b.Explode();
						}
					}
					break;
				}
			}
		}
	}

	public override string ToString()
	{
		return "1 " + Owner.ID + " " + Location.X + " " + Location.Y + " " + Timer + " " + Range;
	}
}
