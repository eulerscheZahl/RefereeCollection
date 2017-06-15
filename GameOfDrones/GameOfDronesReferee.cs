using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

public class GameOfDronesReferee
{
	static int WIDTH = 4000;
	static int HEIGHT = 1800;
	static int ROUNDS = 200;
	static int MIN_DRONES = 3;
	static int MAX_DRONES = 11;
	static int MIN_ZONES = 4;
	static int MAX_ZONES = 8;

	public static void Main(string[] args)
	{
		string path = args.Length == 1 ? args[0] : null;
		int playerCount = int.Parse(Console.ReadLine().Split()[1]);
		Random random = new Random();
		List<Zone> zones = new List<Zone>();
		List<Drone> drones = new List<Drone>();
		int zoneCount = random.Next(Math.Max(MIN_ZONES, 1 + playerCount), MAX_ZONES + 1);
		for (int i = 0; i < zoneCount; i++)
		{
			Point p = new Point();
			do { p = new Point(random.Next(WIDTH), random.Next(HEIGHT)); }
			while (zones.Any(z => Math.Sqrt((z.X - p.X) * (z.X - p.X) + (z.Y - p.Y) * (z.Y - p.Y)) < 700));
			zones.Add(new Zone(i, p.X, p.Y));
		}
		zones = zones.OrderBy(z => z.ID).ToList();

		int droneCount = 1 + random.Next (MIN_DRONES, MAX_DRONES);
		for (int j = 0; j < droneCount; j++)
		{
			drones.Add(new Drone(random.Next(WIDTH), random.Next(HEIGHT), 0));
		}
		for (int i = 1; i < playerCount; i++)
		{
			for (int j = 0; j < droneCount; j++)
			{
				drones.Add(new Drone(drones[j].X, drones[j].Y, i));
			}
		}

		int[] scores = new int[playerCount];
		for (int round = 0; round < ROUNDS; round++)
		{
			if (path != null)
			{
				Bitmap bmp = Draw(zones, drones, scores);
				bmp.Save(path + System.IO.Path.DirectorySeparatorChar + $"{round:000}.png");
				bmp.Dispose();
			}
			List<string> input = zones.Select(z => $"{z.Owner}").ToList();
			input.AddRange(drones.Select(d => $"{d.X} {d.Y}"));
			List<string> actions = new List<string>();
			for (int p = 0; p < playerCount; p++)
			{
				Console.WriteLine("###Input " + p);
				if (round == 0)
				{
					Console.WriteLine($"{playerCount} {p} {droneCount} {zoneCount}");
					foreach (Zone z in zones) Console.WriteLine($"{z.X} {z.Y}");
				}
				foreach (string line in input) Console.WriteLine(line);
				//split in two calls, as brutaltester only allows up to 9 lines
				Console.WriteLine($"###Output {p} {droneCount/2}");
				Console.WriteLine($"###Output {p} {(droneCount+1)/2}");
				for (int l = 0; l < droneCount; l++)
				{
					actions.Add(Console.ReadLine());
				}
			}
			for (int j = 0; j < drones.Count; j++)
			{
				string[] parts = actions[j].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
				drones[j].Move(int.Parse(parts[0]), int.Parse(parts[1]));
			}

			foreach (Zone z in zones)
			{
				int maxCount = 0;
				List<int> maxIndex = new List<int>();
				for (int p = 0; p < playerCount; p++)
				{
					if (maxCount < drones.Count(d => d.Owner == p && z.IsInZone(d)))
					{
						maxCount = drones.Count(d => d.Owner == p && z.IsInZone(d));
						maxIndex.Clear();
					}
					if (maxCount == drones.Count(d => d.Owner == p && z.IsInZone(d))) maxIndex.Add(p);
				}
				if (maxIndex.Count == 1) z.Owner = maxIndex[0];
			}
			for (int j = 0; j < playerCount; j++)
			{
				scores[j] += zones.Count(z => z.Owner == j);
			}
		}
		if (path != null)
		{
			Bitmap bmp = Draw(zones, drones, scores);
			bmp.Save(path + System.IO.Path.DirectorySeparatorChar + $"{ROUNDS:000}.png");
			bmp.Dispose();
		}

		int[] ids = Enumerable.Range(0, playerCount).ToArray();
		Array.Sort(scores, ids);
		string result = "###End " + ids[playerCount - 1];
		for (int i = playerCount - 2; i >= 0; i--)
		{
			if (scores[i + 1] > scores[i])
				result += " ";
			result += ids[i];
		}
		Console.WriteLine(result);
	}

	static Bitmap Draw(List<Zone> zones, List<Drone> drones, int[] scores)
	{
		List<Color> colors = new List<Color> { Color.Gray, Color.Orange, Color.Red, Color.Aquamarine, Color.Purple };
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
					g.FillEllipse(b, d.X - 30, d.Y - 30, 60, 60);
				g.DrawEllipse(Pens.Black, d.X - 30, d.Y - 30, 60, 60);
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

	class Zone
	{
		public int ID;
		public int X;
		public int Y;
		public int Owner = -1;

		public Zone(int id, int x, int y)
		{
			this.ID = id;
			this.X = x;
			this.Y = y;
		}

		public bool IsInZone(Drone d)
		{
			int dx = Math.Abs(this.X - d.X);
			int dy = Math.Abs(this.Y - d.Y);
			return dx * dx + dy * dy <= 100 * 100;
		}
	}

	class Drone
	{
		public int X;
		public int Y;
		public int Owner;

		public Drone(int x, int y, int owner)
		{
			this.X = x;
			this.Y = y;
			this.Owner = owner;
		}

		public Drone(Drone d)
		{
			this.X = d.X;
			this.Y = d.Y;
			this.Owner = d.Owner;
		}

		public void Move(int targetX, int targetY)
		{
			int dx = targetX - X;
			int dy = targetY - Y;
			double dist = Math.Sqrt(dx * dx + dy * dy);
			if (dist <= 100)
			{
				this.X = targetX;
				this.Y = targetY;
				return;
			}
			this.X += (int)(100 * dx / dist);
			this.Y += (int)(100 * dy / dist);
		}
	}
}