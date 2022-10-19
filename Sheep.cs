//#define drawVelocityArrows
//#define drawRadiusOfCOM 
using System.Drawing.Drawing2D;
using System.Security.Cryptography;

namespace Sheep;

/// <summary>
/// Represents a sheep (or white blob with dot for head).
/// The sheep moves with "flock" characteristics, and needs to be part of a flock.
/// </summary>
internal class Sheep
{
    // multipliers
    private const float multiplierCohesion = 0.3f;
    private const float multiplierCohesionWithPredator = -0.7f;
    private const float multiplierSeparation = 0.4f;
    private const float multiplierSeparationWithPredator = -0.9f;
    private const float multiplierAlignment = 0.3f;
    private const float multiplierAlignmentWithPredator = 0.4f;
    private const float multiplierGuidance = 0f;
    private const float multiplierGuidanceWithPredator = 0f;
    private const float multiplierEscape = 3f;

    /// <summary>
    /// Solitary sheep are not happy, so we group them together in a "flock".
    /// </summary>
    private readonly Flock flockSheepIsPartOf;

    /// <summary>
    /// Where the sheep is relative to the sheep pen.
    /// </summary>
    internal PointF Position = new();

    /// <summary>
    /// How fast the sheep is travelling (as a 2d vector) within sheep pen.
    /// </summary>
    internal PointF Velocity = new();

    /// <summary>
    /// The angle the sheep is pointing.
    /// </summary>
    internal float Angle = 0;

    /// <summary>
    /// Even sheep need to eat, if they are then this is set to true.
    /// </summary>
    internal bool IsStopped;

    /// <summary>
    /// Whilst >0, sheep is paused (such as eating).
    /// </summary>
    internal int DurationRemainingStopped;

    /// <summary>
    /// Constructor. Sorry no lamb-das allowed.
    /// </summary>
    /// <param name="flock"></param>
    internal Sheep(Flock flock)
    {
        if (flock is null) throw new ArgumentNullException(nameof(flock), "cannot be null, the sheep are designed to move in a flock");

        flockSheepIsPartOf = flock;

        // randomish place        
        Position = new PointF(RandomNumberGenerator.GetInt32(0, flock.SheepPenWidthPX / 6 + 20),
                              RandomNumberGenerator.GetInt32(0, flock.SheepPenHeightPX / 6) + 40);

        // stationary
        Velocity = new PointF(0, 0);

        // not stopped.
        IsStopped = false;
    }

    /// <summary>
    /// Use to "stop" the sheep for a defined number of frames.
    /// </summary>
    /// <param name="frames"></param>
    internal void MakeSheepPause(int frames = 0)
    {
        if (frames < 1) frames = RandomNumberGenerator.GetInt32(1, 30); // pick a random amount of "pause" frames/time

        DurationRemainingStopped = frames;

        IsStopped = true;
    }

    /// <summary>
    /// The variation in strength of the second multiplier is described by a sigmoid function. 
    /// </summary>
    /// <param name="r">is the value of x where the absolute derivate of p(x) is the largest. This distance r represents the flight zone radius of the sheep.</param>
    /// <param name="x">is the distance from the sheep to the predator </param>
    /// <returns></returns>
    private static double PredatorDistanceSensitiveMultiplier(float r, float x)
    {
        return 1 / Math.PI * Math.Atan((r - x) / 20) + 0.5f;
    }

    /// <summary>
    /// Moves the sheep using flocking/swarming logic.
    /// </summary>
    internal void Move()
    {
        // sheep are grazing or something?
        if (IsStopped)
        {
            if (--DurationRemainingStopped > 0) return;
            IsStopped = false;
        }

        float x = Position.X - Flock.s_predator.X;
        float y = Position.Y - Flock.s_predator.Y;
        float distToPredator = (float)Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) + .00001f;

        float pofx = distToPredator > 150 ? 0 : (float)PredatorDistanceSensitiveMultiplier(150, distToPredator);

        // Craig W. Reynold's flocking behavior is controlled by three simple rules:

        // With these three simple rules, the flock moves in an extremely realistic way, creating
        // complex motion and interaction that would be extremely hard to create otherwise.

        // #1 Cohesion: Steer towards average position of neighbours (long range attraction)
        PointF cohesionVector = flockSheepIsPartOf.MoveTowardsCentreOfMass(this);

        // #2 Separation: avoid crowding neighbours (short range repulsion)
        PointF separationVector = flockSheepIsPartOf.MaintainSeparation(this);

        // #3 Alignment: steer towards average heading of neighbours
        PointF alignmentVector = flockSheepIsPartOf.MatchVelocityOfNearbySheep(this);

        // PLUS rule #4, avoid predators

        // Escape: make them move away from a predator
        PointF escVector = Flock.EscapeTheDog(this, Flock.s_predator);

        // Steer towards a chosen point.
        PointF guidanceVector = new(0, 0); //  flockSheepIsPartOf.EncourageDirectionTowards(new PointF(10, 10), this); // flockSheepIsPartOf.EncourageDirectionAway(new PointF(10, 10), this);

        // This final velocity vector is capped to a certain value vmax that represents the sheep’s maximum velocity. 
        // vmax increases as the predator approaches. If the final velocity vector is below a certain threshold vmin 
        // it is set to zero. The vector is also set to zero if it is directed at a point behind the sheep, as the 
        // sheep can only turn at a certain angular velocity.        

        // apply a "velocity" to our sheep horizontally and vertically based on the "riles"
        Velocity.X += multiplierCohesion * (1 + pofx * multiplierCohesionWithPredator) * cohesionVector.X +
                      multiplierSeparation * (1 + pofx * multiplierSeparationWithPredator) * separationVector.X +
                      multiplierAlignment * (1 + pofx * multiplierAlignmentWithPredator) * alignmentVector.X +
                      multiplierGuidance * (1 + pofx * multiplierGuidanceWithPredator) * guidanceVector.X +
                      multiplierEscape * escVector.X;

        Velocity.Y += multiplierCohesion * (1 + pofx * multiplierCohesionWithPredator) * cohesionVector.Y +
                      multiplierSeparation * (1 + pofx * multiplierSeparationWithPredator) * separationVector.Y +
                      multiplierAlignment * (1 + pofx * multiplierAlignmentWithPredator) * alignmentVector.Y +
                      multiplierGuidance * (1 + pofx * multiplierGuidanceWithPredator) * guidanceVector.Y +
                      multiplierEscape * escVector.Y;

        // velocity drives both desired speed and angle.
        // but a sheep moves forward, not sideways
        double angle = Math.Atan2(Velocity.Y, Velocity.X);

        // work out the speed for that angle
        //float speed = (float)Math.Sqrt(Math.Pow(Velocity.X, 2) + Math.Pow(Velocity.Y, 2));

        Angle = (float)angle.Clamp(Angle - 0.0872665f / 3, Angle + 0.0872665f / 3);

        Flock.StopSheepRunningUnrealisticallyFast(this);

        Position.X += Velocity.X;
        Position.Y += Velocity.Y;

        PointF adjustmentVectorToKeepSheepWithinSheepPen = flockSheepIsPartOf.EnforceBoundary(this);

        Position.X += adjustmentVectorToKeepSheepWithinSheepPen.X;
        Position.Y += adjustmentVectorToKeepSheepWithinSheepPen.Y;

        // http://www.diva-portal.org/smash/get/diva2:675990/FULLTEXT01.pdf

        // occasionally let them rest and eat
        if (RandomNumberGenerator.GetInt32(0, 500) > 492) MakeSheepPause();
    }

    /// <summary>
    /// Draw the sheep in the sheep pen.
    /// 
    /// Initially we create as a filled in white circle, maybe later get a little more fancy.
    /// </summary>
    /// <param name="g"></param>
    internal void Draw(Graphics g, Color colour)
    {
        if (Position.X < 0 || Position.X >= flockSheepIsPartOf.SheepPenWidthPX) return;

        if (Position.Y < 0 || Position.Y >= flockSheepIsPartOf.SheepPenHeightPX) return;

        // draw the sheep, a white blob with black dot for head/
        g.FillEllipse(new SolidBrush(colour), Position.X - 3, Position.Y - 3, 5, 5);

        float x = (float)Math.Cos(Angle) * 3 + Position.X;
        float y = (float)Math.Sin(Angle) * 3 + Position.Y;

        g.DrawRectangle(Pens.Black, x, y, 1, 1);

        // show the predator
        g.FillEllipse(Brushes.Blue, Flock.s_predator.X - 3, Flock.s_predator.Y - 3, 6, 6);


#if drawVelocityArrows
        // velocity arrows
        double angle = Angle;
        float size = (float)Math.Sqrt(Math.Pow(Velocity.X, 2) + Math.Pow(Velocity.Y, 2));

        using Pen p2 = new(Color.DarkSalmon);
        p2.DashStyle = DashStyle.Dot;
        p2.EndCap = LineCap.ArrowAnchor;

        g.DrawLine(p2,
                   (int)Position.X, (int)Position.Y,
                   (int)(20 * size * Math.Cos(angle) + Position.X),
                   (int)(20 * size * Math.Sin(angle) + Position.Y));
#endif

#if drawRadiusOfCOM
        using Pen pen = new(Color.FromArgb(100, 255, 255, 255));
        pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

        g.DrawEllipse(pen, Position.X - Flock.c_sheepCloseEnoughToBeAMass, Position.Y - Flock.c_sheepCloseEnoughToBeAMass, Flock.c_sheepCloseEnoughToBeAMass * 2, Flock.c_sheepCloseEnoughToBeAMass * 2);
#endif
    }
}