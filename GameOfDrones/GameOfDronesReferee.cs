using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;

public class GameOfDronesReferee
{
	static readonly int WIDTH = 4000;
	static readonly int HEIGHT = 1800;
	static readonly int ROUNDS = 200;
	static readonly int ZONE_SIZE = 100;
	static readonly int DRONE_SPEED = 100;
	static readonly int MIN_DRONES = 3;
	static readonly int MAX_DRONES = 11;

	public static void Main(string[] args)
	{
		string path = args.Length == 1 ? args [0] : null;
		Board board = null;
		while (true) {
			string inputLine = Console.ReadLine ();
			if (inputLine.StartsWith ("###Map")) {
				board = new Board ();
			} else if (inputLine.StartsWith ("###Start")) {
				int playerCount = int.Parse (inputLine.Split () [1]);
				if (board == null)
					board = new Board (playerCount);
				while (board.Play (path))
					;
				if (path != null) {
					Bitmap bmp = board.Draw ();
					bmp.Save (path + Path.DirectorySeparatorChar +$"{ROUNDS:000}.png");
					bmp.Dispose ();
				}

				Console.WriteLine (board.DeclareWinner ());
				return;
			}
		}
	}

	class Board {
		private int playerCount;
		private int droneCount;
		private List<Zone> zones = new List<Zone>();
		private List<Drone> drones = new List<Drone>();
		private int[] scores;
		private int round = 0;

		public Board() {
			int[] nums = Console.ReadLine ().Split (' ').Select (int.Parse).ToArray ();
			playerCount = nums[0];
			droneCount = nums[2];
			int zoneCount = nums[3];
			for (int i = 0; i < zoneCount; i++) {
				nums = Console.ReadLine ().Split (' ').Select (int.Parse).ToArray ();
				zones.Add(new Zone(nums[0], nums[1]));
			}
			for (int i = 0; i < zoneCount; i++) {
				Console.ReadLine();
			}
			for (int p = 0; p < playerCount; p++) {
				for (int d = 0; d < droneCount; d++) {
					nums = Console.ReadLine ().Split (' ').Select (int.Parse).ToArray ();
					drones.Add(new Drone(nums[0], nums[1], p));
				}
			}
			scores = new int[playerCount];
		}

		public Board(int playerCount) {
			if (this.playerCount > 0) return; //already loaded a map
			this.playerCount = playerCount;
			Random random = new Random ();
			int zoneCount = 2 * playerCount;
			for (int i = 0; i < zoneCount; i++) {
				Point p = new Point ();
				do {
					p = new Point (random.Next (WIDTH), random.Next (HEIGHT));
				} while (zones.Any (z => Math.Sqrt ((z.X - p.X) * (z.X - p.X) + (z.Y - p.Y) * (z.Y - p.Y)) < 700));
				zones.Add (new Zone (p.X, p.Y));
			}

			droneCount = 1 + random.Next (MIN_DRONES, MAX_DRONES);
			for (int j = 0; j < droneCount; j++) {
				drones.Add (new Drone (random.Next (WIDTH), random.Next (HEIGHT), 0));
			}
			for (int i = 1; i < playerCount; i++) {
				for (int j = 0; j < droneCount; j++) {
					drones.Add (new Drone (drones [j].X, drones [j].Y, i));
				}
			}
			scores = new int[playerCount];
		}

		public bool Play(string path) {
			List<string> input = zones.Select (z => $"{z.Owner}").ToList ();
			input.AddRange (drones.Select (d => d.ToString()));
			if (path != null) {
				Bitmap bmp = Draw ();
				bmp.Save (path + Path.DirectorySeparatorChar +$"{round:000}.png");
				bmp.Dispose ();

				using (StreamWriter sw = new StreamWriter ($"{round:000}.txt")) { 
					sw.WriteLine ($"{playerCount} {0} {droneCount} {zones.Count}");
					foreach (Zone z in zones)
						sw.WriteLine ($"{z.X} {z.Y}");
					foreach (string line in input)
						sw.WriteLine (line);
				}
			}
			List<string> actions = new List<string> ();
			for (int p = 0; p < playerCount; p++) {
				Console.WriteLine ("###Input " + p);
				if (round == 0) {
					Console.WriteLine ($"{playerCount} {p} {droneCount} {zones.Count}");
					foreach (Zone z in zones)
						Console.WriteLine ($"{z.X} {z.Y}");
				}
				foreach (string line in input)
					Console.WriteLine (line);
				Console.WriteLine ($"###Output {p} {droneCount}");
				for (int l = 0; l < droneCount; l++) {
					actions.Add (Console.ReadLine ());
				}
			}
			for (int j = 0; j < drones.Count; j++) {
				string[] parts = actions [j].Split (" ".ToCharArray (), StringSplitOptions.RemoveEmptyEntries);
				drones [j].Move (int.Parse (parts [0]), int.Parse (parts [1]));
			}

			foreach (Zone z in zones) {
				int maxCount = 0;
				List<int> maxIndex = new List<int> ();
				for (int p = 0; p < playerCount; p++) {
					if (maxCount < drones.Count (d => d.Owner == p && z.IsInZone (d))) {
						maxCount = drones.Count (d => d.Owner == p && z.IsInZone (d));
						maxIndex.Clear ();
					}
					if (maxCount == drones.Count (d => d.Owner == p && z.IsInZone (d)))
						maxIndex.Add (p);
				}
				if (maxIndex.Count == 1)
					z.Owner = maxIndex [0];
			}
			for (int j = 0; j < playerCount; j++) {
				scores [j] += zones.Count (z => z.Owner == j);
			}
			round++;
			return round < 200;
		}

		public Bitmap Draw()
		{
			Color[] colors = { Color.Gray, Color.Orange, Color.Red, Color.Aquamarine, Color.Purple };
			Bitmap bmp = new Bitmap(WIDTH, HEIGHT + 50);
			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.Clear(Color.White);
				foreach (Zone z in zones)
				{
					using (Brush b = new SolidBrush(colors[z.Owner + 1]))
						g.FillEllipse(b, z.X - 100, z.Y - 100, 200, 200);
				}
				foreach (Drone d in drones)
				{
					using (Brush b = new SolidBrush(colors[d.Owner + 1]))
						g.FillEllipse(b, (float)d.X - 30, (float)d.Y - 30, 60, 60);
					g.DrawEllipse(Pens.Black, (float)d.X - 30, (float)d.Y - 30, 60, 60);
				}
				g.FillRectangle(Brushes.Gray, 0, HEIGHT, WIDTH, 50);
				for (int i = 0; i < scores.Length; i++)
				{
					using (Brush b = new SolidBrush(colors[i + 1]))
						g.DrawString(scores[i].ToString(), new Font(new FontFamily("Arial"), 30), b, WIDTH / 2 + 200 * (i - scores.Length / 2f), HEIGHT + 5);
				}
			}
			return bmp;
		}

		public string DeclareWinner() {
			int[] ids = Enumerable.Range (0, playerCount).ToArray ();
			Array.Sort (scores, ids);
			string result = "###End " + ids [playerCount - 1];
			for (int i = playerCount - 2; i >= 0; i--) {
				if (scores [i + 1] > scores [i])
					result += " ";
				result += ids [i];
			}
			return result;
		}
	}

	class Zone
	{
		public int X { get; private set; }
		public int Y { get; private set; }
		public int Owner { get; set; } = -1;

		public Zone(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}

		public bool IsInZone(Drone d)
		{
			double dx = Math.Abs(this.X - d.X);
			double dy = Math.Abs(this.Y - d.Y);
			return dx * dx + dy * dy <= ZONE_SIZE * ZONE_SIZE;
		}
	}

	class Drone
	{
		public double X { get; private set; }
		public double Y { get; private set; }
		public int Owner { get; private set; }

		public Drone(double x, double y, int owner)
		{
			this.X = x;
			this.Y = y;
			this.Owner = owner;
		}

		public void Move(int targetX, int targetY)
		{
			double dx = targetX - X;
			double dy = targetY - Y;
			double dist = Math.Sqrt(dx * dx + dy * dy);
			if (dist <= DRONE_SPEED)
			{
				this.X = targetX;
				this.Y = targetY;
				return;
			}
			this.X += dx / dist * DRONE_SPEED;
			this.Y += dy / dist * DRONE_SPEED;
		}

		public override string ToString ()
		{
			return Math.Round(X) + " " + Math.Round(Y);
		}
	}
}