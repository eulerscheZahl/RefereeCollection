// this is not a complete referee on purpose, as the game is at least 95% about writing a working simulation
// I do not guarantee that it is bugfree either, but it can't be off too far
// for collisions see also: http://files.magusgeek.com/csb/csb_en.html

private void Simulate()
{
	double epsilon = 1e-9;
	foreach (Chip c in Chips.Where(c => c.MovementPlan != null).ToList())
	{
		//CREATE NEW DROPS
		double alpha = Math.Atan2(c.MovementPlan.Y - c.Y, c.MovementPlan.X - c.X);
		double radius = c.Radius * Math.Sqrt(1.0 / 15);
		c.Radius *= Math.Sqrt(14.0 / 15);
		double dropX = c.X - (c.Radius - radius) * Math.Cos(alpha);
		double dropY = c.Y - (c.Radius - radius) * Math.Sin(alpha);
		double dropVx = c.VX - 200 * Math.Cos(alpha);
		double dropVy = c.VY - 200 * Math.Sin(alpha);
		c.VX += 200 * Math.Cos(alpha) / 14;
		c.VY += 200 * Math.Sin(alpha) / 14;

		Chip drop = new Chip(-1, dropX, dropY, dropVx, dropVy, radius);
		drop.Parent = c;
		Chips.Add(drop);
	}
	
	double time = 0;
	while (time < 1)
	{
		List<Collision> collisions = new List<Collision>();
		for (int c1Index = 0; c1Index < Chips.Count; c1Index++)
		{
			Chip chip = Chips[c1Index];
			for (int c2Index = c1Index + 1; c2Index < Chips.Count; c2Index++)
			{
				if (chip.Parent == Chips[c2Index].Parent) continue;
				Collision c = Collision.Collide(chip, Chips[c2Index]);
				if (c != null) collisions.Add(c);
			}
			Collision collideEdge = chip.CollideEdge();
			collisions.Add(collideEdge);
		}
		collisions.Sort();

		Collision collision = collisions.FirstOrDefault();
		List<Collision> newCollisions = new List<Collision>();
		double delta = 1 - time;
		if (collision != null && collision.Time < 0)
			collision.Time = 0; //already touching because of multi absorbtion
		if (collision != null) delta = Math.Min(delta, collision.Time);
		delta += epsilon;
		time += delta;
		foreach (Chip c in Chips) c.Move(delta);
		Chip c1 = collision?.C1;
		Chip c2 = collision?.C2;

		if (c2 != null && collision.Time <= delta) //real collision, not just edge
		{
			c2.Parent = c2;
			double m1 = c1.Radius * c1.Radius;
			double m2 = c2.Radius * c2.Radius;
			if (c1.Radius == c2.Radius)
			{
				double mcoeff = (m1 + m2) / (m1 * m2);
				double nx = c1.X - c2.X;
				double ny = c1.Y - c2.Y;
				double nxnysquare = nx * nx + ny * ny;
				double dvx = c1.VX - c2.VX;
				double dvy = c1.VY - c2.VY;
				double product = nx * dvx + ny * dvy;
				double fx = (nx * product) / (nxnysquare * mcoeff);
				double fy = (ny * product) / (nxnysquare * mcoeff);
				c1.VX -= fx / m1;
				c1.VY -= fy / m1;
				c2.VX += fx / m2;
				c2.VY += fy / m2;
				double impulse = Math.Sqrt(fx * fx + fy * fy);
				c1.VX -= fx / m1;
				c1.VY -= fy / m1;
				c2.VX += fx / m2;
				c2.VY += fy / m2;

				c1.Move(epsilon);
				c2.Move(epsilon);
			}
			else
			{
				Chips.Remove(c1);
				Chips.Remove(c2);

				double x = c1.X + (c2.X - c1.X) * m2 / (m1 + m2);
				double y = c1.Y + (c2.Y - c1.Y) * m2 / (m1 + m2);
				double vx = (c1.VX * m1 + c2.VX * m2) / (m1 + m2);
				double vy = (c1.VY * m1 + c2.VY * m2) / (m1 + m2);
				Chip combined = new Chip(c1.Radius > c2.Radius ? c1.Player : c2.Player, c1.Radius > c2.Radius ? c1.ID : c2.ID, x, y, vx, vy, Math.Sqrt(m1 + m2));
				combined.Move(epsilon); //get away from edge
				Chips.Add(combined);
			}
		}
	}
	Chips.Sort((a, b) => a.ID.CompareTo(b.ID));
}

class Chip
{
	public int ID { get; private set; }
	public int Player;
	public double X { get; private set; }
	public double Y { get; private set; }
	public double VX;
	public double VY;
	public double Radius;
	public Point MovementPlan = null;
	public Chip Parent;
	private static int counter = 1;

	public Chip(int player, double x, double y, double vx, double vy, double radius) : this(player, counter++, x, y, vx, vy, radius) { }

	public Chip(int player, int id, double x, double y, double vx, double vy, double radius)
	{
		this.ID = id;
		this.Player = player;
		this.X = x;
		this.Y = y;
		this.VX = vx;
		this.VY = vy;
		this.Radius = radius;
		this.Parent = this;
	}

	public Collision CollideEdge()
	{
		double collideEdge = double.MaxValue;
		if (this.VX > 0) collideEdge = (Board.WIDTH - this.X - this.Radius) / this.VX;
		else if (this.VX < 0) collideEdge = -(this.X - this.Radius) / this.VX;
		if (this.VY > 0 && (Board.HEIGHT - this.Y - this.Radius) / this.VY < collideEdge) collideEdge = Math.Min(collideEdge, (Board.HEIGHT - this.Y - this.Radius) / this.VY);
		else if (this.VY < 0 && -(this.Y - this.Radius) / this.VY < collideEdge) collideEdge = Math.Min(collideEdge, -(this.Y - this.Radius) / this.VY);
		return new Collision(this, null, collideEdge);
	}

	public void Move(double time)
	{
		this.X += time * VX;
		this.Y += time * VY;
		if (X < Radius || X == Radius && time > 0)
		{
			X = Radius;
			VX *= -1;
			this.Parent = this;
		}
		if (X + Radius > Board.WIDTH || X + Radius == Board.WIDTH && time > 0)
		{
			X = Board.WIDTH - Radius;
			VX *= -1;
			this.Parent = this;
		}
		if (Y < Radius || Y == Radius && time > 0)
		{
			Y = Radius;
			VY *= -1;
			this.Parent = this;
		}
		if (Y + Radius > Board.HEIGHT || Y + Radius == Board.HEIGHT && time > 0)
		{
			Y = Board.HEIGHT - Radius;
			VY *= -1;
			this.Parent = this;
		}
	}
}

