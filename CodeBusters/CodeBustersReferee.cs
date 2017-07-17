using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

class CodeBustersReferee
{
	public static void Main(string[] args)
	{
		string path = args.Length == 1 ? args[0] : null;

		//read ###Start 2, as there are 2 players
		Console.ReadLine();

		Board board = new Board();
		while (board.Play())
		{
			board.Tick();
			if (path != null) {
				Bitmap bmp = board.Draw ();
				bmp.Save (path + System.IO.Path.DirectorySeparatorChar +$"{board.Round:000}.png");
				bmp.Dispose ();
			}
		}
		board.DeclareWinner();
	}

	class Board
	{
		public static readonly int WIDTH = 16000;
		public static readonly int HEIGHT = 9000;
		public static readonly int MAX_ROUNDS = 250;
		private static Random random = new Random();
		private List<Buster> busters = new List<Buster>();
		private List<Ghost> ghosts = new List<Ghost>();
		public int Round { get; private set; }
		Player p1 = new Player(0);
		Player p2 = new Player(1);
		int bustersPreTeam = random.Next(2, 5);
		int ghostCount = 1 + 2 * random.Next(4, 13);

		public Board()
		{            
			ghosts.Add(new Ghost(0, WIDTH / 2, HEIGHT / 2, random));
			while(ghosts.Count < ghostCount)
			{
				int x = 0, y = 0;
				do
				{
					x = random.Next(WIDTH / 2);
					y = random.Next(HEIGHT);
				} while (Math.Sqrt(x * x + y * y) < 4000);
				Ghost g1 = new Ghost(ghosts.Count, x, y, random);
				ghosts.Add(g1);
				ghosts.Add(new Ghost(g1));
			}

			List<Point>[] startLocations = new List<Point>[] {
				new List<Point>(), new List<Point>(),
				new List<Point> { new Point(1176, 2024), new Point(2024, 1176) },
				new List<Point> { new Point(1600, 1600), new Point(2449, 751), new Point(751, 2449) },
				new List<Point> { new Point(1176, 2024), new Point(2024, 1176), new Point(327, 2873), new Point(2873, 327) }
			};
			foreach (Point p in startLocations[bustersPreTeam]) {
				busters.Add(new Buster(busters.Count, p.X, p.Y, p1));
			}
			for (int i = 0; i < bustersPreTeam; i++)
			{
				busters.Add(new Buster(i + bustersPreTeam, WIDTH - busters[i].X, HEIGHT - busters[i].Y, p2));
			}
		}

		public void Tick()
		{
			List<string> actions = new List<string> ();
			for (int id = 0; id <= 1; id++) {
				Console.WriteLine ($"###Input {id}");
				List<string> output = new List<string> ();
				output.AddRange (busters.Where (b => b.Player.ID == id || b.Viewers.Any (v => v.Player.ID == id)).Select (b => b.ToString ()));
				output.AddRange (ghosts.Where (g => g.Viewers.Any (v => v.Player.ID == id)).Select (g => g.ToString ()));
				output.Insert (0, output.Count.ToString ());
				if (Round == 1) {
					output.Insert (0, $"{bustersPreTeam}");
					output.Insert (1, $"{ghostCount}");
					output.Insert (2, $"{id}");
				}
				Console.WriteLine (string.Join (Environment.NewLine, output));
				Console.WriteLine ($"###Output {id} {bustersPreTeam}");
				for (int i = 0; i < bustersPreTeam; i++) {
					actions.Add (Console.ReadLine ());
				}
			}

			foreach (GameObject g in ghosts.Union<GameObject>(busters)) {
				g.PreviousViewers = g.Viewers;
				g.Viewers = new List<Buster> ();
			}
			foreach (Ghost g in ghosts)
				g.Attackers.Clear ();

			bool[] apply = busters.Select (b => !b.IsStunned ()).ToArray ();
			for (int i = 0; i < busters.Count; i++) {
				busters [i].ApplyAction (ghosts, busters, actions [i], apply [i]);
			}
			foreach (Buster b in busters) {
				if (b.IsStunned () && b.BustedGhost != null)
					b.Release (ghosts);
			}

			for (int i = ghosts.Count - 1; i >= 0; i--) {
				Ghost g = ghosts [i];
				if (g.Attackers.Count (a => a.Player == p1) > g.Attackers.Count (a => a.Player == p2))
					CaptureGhost (g, g.Attackers.Where (a => a.Player == p1).ToList ());
				if (g.Attackers.Count (a => a.Player == p1) < g.Attackers.Count (a => a.Player == p2))
					CaptureGhost (g, g.Attackers.Where (a => a.Player == p2).ToList ());
				if (g.Attackers.Count == 0) { //escape
					List<Buster> closestBusters = g.PreviousViewers.OrderBy (b => b.Dist (g)).Where (b => b.Dist (g) <= Buster.BUSTER_VIEW).ToList ();
					if (closestBusters.Count > 0) {
						closestBusters = closestBusters.Where (b => b.Dist (g) == closestBusters [0].Dist (g)).ToList ();
						int avgX = (int)closestBusters.Average (b => b.X);
						int avgY = (int)closestBusters.Average (b => b.Y);
						g.Flee (new Point (avgX, avgY));
					}
				}
			}
		}

		public Bitmap Draw()
		{
			int scaleDiv = 10;
			Bitmap bmp = new Bitmap (WIDTH / scaleDiv, HEIGHT / scaleDiv);
			using (Graphics g = Graphics.FromImage (bmp)) {
				g.Clear (Color.White);
				foreach (Buster b in busters) {
					if (b.IsStunned ())
						g.FillEllipse (Brushes.Blue, b.X / scaleDiv - 50, b.Y / scaleDiv - 50, 100, 100);
					g.FillEllipse (b.Player.ID == 0 ? Brushes.Orange : Brushes.Red, b.X / scaleDiv - 30, b.Y / scaleDiv - 30, 60, 60);
					g.DrawEllipse (b.Player.ID == 0 ? Pens.Orange : Pens.Red, (b.X - b.ViewRange) / scaleDiv, (b.Y - b.ViewRange) / scaleDiv, 2 * b.ViewRange / scaleDiv, 2 * b.ViewRange / scaleDiv);
					if (b.BustedGhost != null)
						g.FillEllipse (Brushes.Green, b.X / scaleDiv - 15, b.Y / scaleDiv - 15, 30, 30);
				}
				foreach (Ghost ghost in ghosts) {
					g.FillEllipse (Brushes.Green, ghost.X / scaleDiv - 30, ghost.Y / scaleDiv - 30, 60, 60);
					foreach (Buster b in ghost.Attackers)
						g.DrawLine (Pens.Red, ghost.X / scaleDiv, ghost.Y / scaleDiv, b.X / scaleDiv, b.Y / scaleDiv);
					g.DrawString (ghost.Stamina.ToString (), new Font ("Arial", 12), Brushes.Black, ghost.X / scaleDiv, ghost.Y / scaleDiv);
				}
			}
			return bmp;
		}

		private void CaptureGhost(Ghost g, List<Buster> attackers)
		{
			if (g.Stamina > 0)
				return;
			Buster b = attackers.OrderBy (a => a.Dist (g)).First ();
			b.BustedGhost = g;
			ghosts.Remove (g);
		}

		public bool Play()
		{
			return Round++ < MAX_ROUNDS && p1.CaughtGhosts * 2 < ghostCount && p2.CaughtGhosts * 2 < ghostCount;
		}

		public void DeclareWinner()
		{
			if (p1.Score(busters) > p2.Score(busters))
				Console.WriteLine("###End 0 1");
			else if (p1.Score(busters) < p2.Score(busters))
				Console.WriteLine("###End 1 0");
			else
				Console.WriteLine("###End 01");
		}
	}

	class Buster : GameObject
	{
		public static readonly int BUSTER_MAX_DIST = 800;
		public static readonly int BUSTER_VIEW = 2200;
		public static readonly int BUSTER_RADAR = 4400;
		public static readonly int BUSTER_MAX_STUN_RANGE = 1760;
		public static readonly int BUSTER_STUN_COOLDOWN = 20;
		public static readonly int BUSTER_STUN_HIT = 10;
		public static readonly int BUSTER_MIN_BUST = 900;
		public static readonly int BUSTER_MAX_BUST = 1760;
		public static readonly int BUSTER_MAX_EJECT = 1760;
		public static readonly int BUSTER_RELEASE_RANGE = 1600;

		public Ghost BustedGhost { get; set; }
		public Player Player { get; private set; }
		public int ViewRange { get; private set; }
		private int cooldownTime = 0;
		private int stunnedTime = 0;
		private bool canRadar = true;
		public Buster(int id, int x, int y, Player player) : base(id, x, y) { this.Player = player; }

		public bool IsStunned()
		{
			return stunnedTime > 0;
		}

		public void ApplyAction(List<Ghost> ghosts, List<Buster> busters, string action, bool apply)
		{
			string[] parts = action.Split (" ".ToCharArray (), StringSplitOptions.RemoveEmptyEntries);
			ViewRange = BUSTER_VIEW;
			if (cooldownTime > 0)
				cooldownTime--;
			if (stunnedTime > 0)
				stunnedTime--;
			if (apply) {// buster is not stunned
				switch (parts [0].ToUpper ()) {
				case "MOVE":
					this.Move (new Point (int.Parse (parts [1]), int.Parse (parts [2])));
					break;
				case "BUST":
					DropGhost (ghosts);
					Ghost ghost = ghosts.FirstOrDefault (g => g.ID == int.Parse (parts [1]));
					if (ghost != null && this.Dist (ghost) >= BUSTER_MIN_BUST && this.Dist (ghost) <= BUSTER_MAX_BUST) {
						ghost.Attack (this);
					}
					break;
				case "RELEASE":
					Release (ghosts);
					break;
				case "STUN":
					DropGhost (ghosts);
					Buster target = busters.FirstOrDefault (b => b.ID == int.Parse (parts [1]));
					if (cooldownTime == 0 && this.Dist (target) <= BUSTER_MAX_STUN_RANGE) {
						target.stunnedTime = BUSTER_STUN_HIT;
						if (target.ID > this.ID)
							target.stunnedTime++; //will be reduced during target.ApplyAction
						this.cooldownTime = BUSTER_STUN_COOLDOWN;
					}
					break;
				case "RADAR":
					if (canRadar) {
						canRadar = false;
						ViewRange = BUSTER_RADAR;
					}
					break;
				case "EJECT":
					DropGhost (ghosts)?.MoveTo (new Point (int.Parse (parts [1]), int.Parse (parts [2])), BUSTER_MAX_EJECT);
					break;
				}
			}
			foreach (GameObject g in ghosts.Union<GameObject>(busters)) {
				if (this.Dist (g) <= ViewRange)
					g.See (this);
			}
		}

		private void Move(Point target) => MoveTo(target, BUSTER_MAX_DIST);

		private Ghost DropGhost(List<Ghost> ghosts)
		{
			if (BustedGhost != null) {
				Ghost g = BustedGhost;
				ghosts.Add (g);
				g.X = this.X;
				g.Y = this.Y;
				g.Attackers.Clear ();
				BustedGhost = null;
				return g;
			}
			return null;
		}

		public void Release(List<Ghost> ghosts)
		{
			if (BustedGhost == null)
				return;
			if (this.Dist (Player.BaseLocation) <= BUSTER_RELEASE_RANGE)
				Player.ScorePoint ();
			else {
				BustedGhost.X = this.X;
				BustedGhost.Y = this.Y;
				BustedGhost.Attackers.Clear ();
				ghosts.Add (BustedGhost);
			}

			BustedGhost = null;
		}

		public override string ToString()
		{
			int state = 0;
			if (BustedGhost != null)
				state = 1;
			if (IsStunned ())
				state = 2;
			int value = -1;
			if (BustedGhost != null)
				value = BustedGhost.ID;
			return $"{ID} {X} {Y} {Player.ID} {state} {value}";
		}
	}

	class Player
	{
		public int ID { get; private set; }
		public GameObject BaseLocation;
		public int CaughtGhosts { get; private set; }

		public Player(int id)
		{
			this.ID = id;
			if (id == 0) BaseLocation = new GameObject(0, 0, 0);
			else BaseLocation = new GameObject(0, Board.WIDTH, Board.HEIGHT);
		}

		public int Score(List<Buster> busters)
		{
			return CaughtGhosts + busters.Count(b => b.Player == this && b.BustedGhost != null);
		}

		public void ScorePoint()
		{
			CaughtGhosts++;
		}
	}

	class Ghost : GameObject
	{
		public static readonly int ESCAPE_SPEED = 400;

		public int Stamina { get; set; }
		public List<Buster> Attackers = new List<Buster>();
		private static readonly int[] STAMINA = new int[] { 3, 15, 40 };


		public Ghost(int id, int x, int y, Random random) : base(id, x, y)
		{
			Stamina = STAMINA[random.Next(STAMINA.Length)];
		}

		public Ghost(Ghost g) : base(g.ID + 1, Board.WIDTH - g.X, Board.HEIGHT - g.Y)
		{
			this.Stamina = g.Stamina;
		}

		public void Attack(Buster b)
		{
			Attackers.Add(b);
			Stamina = Math.Max(0, Stamina - 1);
		}

		public void Flee(Point p) {
			int targetX = this.X + ESCAPE_SPEED * (this.X - p.X);
			int targetY = this.Y + ESCAPE_SPEED * (this.Y - p.Y);
			MoveTo (new Point (targetX, targetY), ESCAPE_SPEED);
		}

		public override string ToString()
		{
			return $"{ID} {X} {Y} -1 {Stamina} {Attackers.Count}";
		}
	}


	class GameObject
	{
		public int X { get; set; }
		public int Y { get; set; }
		public int ID { get; private set; }
		public List<Buster> Viewers = new List<Buster>();
		public List<Buster> PreviousViewers = new List<Buster>();
		public GameObject(int id, int x, int y)
		{
			this.ID = id;
			this.X = x;
			this.Y = y;
		}

		public void MoveTo(Point target, int dist)
		{
			double dx = target.X - this.X;
			double dy = target.Y - this.Y;
			double d = Math.Sqrt (dx * dx + dy * dy);
			if (d <= dist) {
				this.X = target.X;
				this.Y = target.Y;
			} else {
				this.X = (int)Math.Round (this.X + dist * dx / d);
				this.Y = (int)Math.Round (this.Y + dist * dy / d);
			}
			ResetInBoard ();
		}

		private void ResetInBoard()
		{
			if (X < 0)
				X = 0;
			if (X > Board.WIDTH)
				X = Board.WIDTH;
			if (Y < 0)
				Y = 0;
			if (Y > Board.HEIGHT)
				Y = Board.HEIGHT;
		}

		public double Dist(GameObject other)
		{
			double dx = this.X - other.X;
			double dy = this.Y - other.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}

		public void See(Buster b)
		{
			Viewers.Add(b);
		}
	}
}
