using System.Drawing.Drawing2D;

namespace Sheep;

/// <summary>
/// Class represents a flock of "sheep". 
/// 
/// Flocking: https://en.wikipedia.org/wiki/Flocking_(behavior)
/// 
/// "Flocking" is the collective motion by a group of self-propelled entities and is a collective animal behavior 
/// exhibited by many living beings such as birds, fish, bacteria, and insects.
/// It is considered an emergent behavior arising from simple rules that are followed by individuals and does not 
/// involve any central coordination.
///  
/// https://vergenet.net/~conrad/boids/pseudocode.html article helped a lot to become proficient at flocking.
/// </summary>
internal class Flock
{
    #region CONSTANTS
    // all should be prefixed with "c_".

    /// <summary>
    /// If the sheep is going slower than this, we set it to 0.
    /// </summary>
    private const float c_minimumSheepSpeedBeforeStop = 0.1f;

    /// <summary>
    /// Sheep can run, but like all animals they are limited by physics and physiology.
    /// This controls the amount sheep can move per frame. It assumes each sheep is 
    /// comparable in performance.
    /// </summary>
    private const float c_maximumSheepVelocityInAnyDirection = 0.7f;

    /// <summary>
    /// How close a sheep can sense all the other sheep (makes the clump).
    /// </summary>
    internal const float c_sheepCloseEnoughToBeAMass = 50;

    /// <summary>
    /// Defines how many sheep we create.
    /// </summary>
    private const int c_initialFlockSize = 50;

    /// <summary>
    /// Coloured sheep? A bit too much dye.
    /// </summary>
    private readonly Color c_sheepColour = Color.Pink;
    #endregion

    #region STATIC VARIABLES 
    // all should be prefixed with "s_".

    /// <summary>
    /// Lines the sheep must avoid (makes for more of a challenge). Static as it applies to ALL flocks.
    /// </summary>
    internal static List<PointF[]> s_lines = new();

    /// <summary>
    /// Region that is the "home" scoring zone. Static as it applies to ALL flocks.
    /// </summary>
    internal static RectangleF s_sheepPenScoringZone = new(100, 100, 100, 100);

    /// <summary>
    /// Brush for drawing scoring zone. Static as it applies to ALL flocks.
    /// </summary>
    private readonly static HatchBrush s_hatchBrushForScoringZone = new(HatchStyle.DiagonalCross, Color.FromArgb(30, 255, 255, 255), Color.Transparent);

    /// <summary>
    /// Where the predator is located.
    /// </summary>
    internal static PointF s_predator = new(100, 100);
    #endregion

    /// <summary>
    /// Horizontal confines of the sheep pen in pixels.
    /// </summary>
    internal int SheepPenWidthPX;

    /// <summary>
    /// Vertical confines of the sheep pen in pixels.
    /// </summary>
    internal int SheepPenHeightPX;

    /// <summary>
    /// Represents the flock of sheep.
    /// </summary>
    readonly List<Sheep> sheepInTheflock = new();

    /// <summary>
    /// Constructor: Creates a flock of sheep.
    /// </summary>
    internal Flock(int width, int height)
    {
        SheepPenWidthPX = width;
        SheepPenHeightPX = height;

        // add the required number of sheep
        for (int i = 0; i < c_initialFlockSize; i++)
        {
            sheepInTheflock.Add(new Sheep(this));
        }
    }

    /// <summary>
    /// Computes center of ALL sheep except this one.
    /// </summary>
    internal PointF CenterOfMassOfFlock(Sheep thisSheep)
    {
        // center of nothing = nothing
        if (sheepInTheflock.Count < 2) return new PointF(thisSheep.Position.X, thisSheep.Position.Y);

        // compute center
        float x = 0;
        float y = 0;

        foreach (Sheep sheep in sheepInTheflock)
        {
            if (sheep == thisSheep) continue; // center of mass excludes this sheep

            x += sheep.Position.X;
            y += sheep.Position.Y;
        }

        // we exclude "thisSheep", so average is N-1.
        int sheepSummed = sheepInTheflock.Count - 1;

        return new PointF(x / sheepSummed, y / sheepSummed);
    }

    /// <summary>
    /// Pythagoras's theorem to work out distance between to points.
    /// a^2 = b^2 + c^2
    /// </summary>
    /// <param name="point1"></param>
    /// <param name="point2"></param>
    /// <returns></returns>
    internal static float DistanceBetweenPoints(PointF point1, PointF point2)
    {
        return (float)Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2));
    }

    /// <summary>
    /// Stops the sheep going too fast.
    /// </summary>
    /// <param name="thisSheep"></param>
    internal static void StopSheepRunningUnrealisticallyFast(Sheep thisSheep)
    {
        // Pythagoras to turn x & y velocity into a diagonal velocity
        float velocity = (float)Math.Sqrt(Math.Pow(thisSheep.Velocity.X, 2) + Math.Pow(thisSheep.Velocity.Y, 2));

        // no adjustment required?
        if (velocity < c_maximumSheepVelocityInAnyDirection) return;

        // we need to reduce the speed
        thisSheep.Velocity.X = thisSheep.Velocity.X / velocity * c_maximumSheepVelocityInAnyDirection;
        if (Math.Abs(thisSheep.Velocity.X) < c_minimumSheepSpeedBeforeStop) thisSheep.Velocity.X = 0;

        thisSheep.Velocity.Y = thisSheep.Velocity.Y / velocity * c_maximumSheepVelocityInAnyDirection;
        if (Math.Abs(thisSheep.Velocity.Y) < c_minimumSheepSpeedBeforeStop) thisSheep.Velocity.Y = 0;
    }

    /// <summary>
    /// Localised, single wind for all sheep. If it were different per sheep they would fast
    /// become a misaligned mess.
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    internal static PointF Wind()
    {
        return new(0, 0);
    }

    /// <summary>
    /// Prevent sheep moving off-screen.
    /// </summary>
    /// <param name="thisSheep"></param>
    /// <returns></returns>
    internal PointF EnforceBoundary(Sheep thisSheep)
    {
        PointF p = new(0, 0);

        if (thisSheep.Position.X < 5) p.X = 5;

        if (thisSheep.Position.X > SheepPenWidthPX - 5) p.X = -5;

        if (thisSheep.Position.Y < 5) p.Y = 5;

        if (thisSheep.Position.Y > SheepPenHeightPX - 5) p.Y = -5;

        return p;
    }

    #region SHEEP RULES OF MOVEMENT
    /// <summary>
    /// Rule 1: Sheep like to move towards the centre of mass of flock.
    /// </summary>
    /// <param name="thisSheep"></param>
    /// <returns></returns>
    internal PointF MoveTowardsCentreOfMass(Sheep thisSheep)
    {
        //      _  _   _    ____      _               _                __    _   _                  _   _             
        //    _| || |_/ |  / ___|___ | |__   ___  ___(_) ___  _ __    / /_ _| |_| |_ _ __ __ _  ___| |_(_) ___  _ __  
        //   |_  ..  _| | | |   / _ \| '_ \ / _ \/ __| |/ _ \| '_ \  / / _` | __| __| '__/ _` |/ __| __| |/ _ \| '_ \ 
        //   |_      _| | | |__| (_) | | | |  __/\__ \ | (_) | | | |/ / (_| | |_| |_| | | (_| | (__| |_| | (_) | | | |
        //     |_||_| |_|  \____\___/|_| |_|\___||___/_|\___/|_| |_/_/ \__,_|\__|\__|_|  \__,_|\___|\__|_|\___/|_| |_|
        //                                                                                                            
        // Cohesion: Steer towards average position of neighbours (long range attraction)

        PointF pointF = CenterOfMassOfFlock(thisSheep);

        // move it 1% of the way towards the centre
        return new PointF((pointF.X - thisSheep.Position.X) / 100,
                          (pointF.Y - thisSheep.Position.Y) / 100);
    }

    /// <summary>
    /// Rule 2: Boids try to keep a small distance away from other objects (including other boids).
    /// </summary>
    /// <param name="thisSheep"></param>
    /// <returns></returns>
    internal PointF MaintainSeparation(Sheep thisSheep)
    {
        //      _  _  ____       _             _     _                             _ _             
        //    _| || ||___ \     / \__   _____ (_) __| |   ___ _ __ _____      ____| (_)_ __   __ _ 
        //   |_  ..  _|__) |   / _ \ \ / / _ \| |/ _` |  / __| '__/ _ \ \ /\ / / _` | | '_ \ / _` |
        //   |_      _/ __/   / ___ \ V / (_) | | (_| | | (__| | | (_) \ V  V / (_| | | | | | (_| |
        //     |_||_||_____| /_/   \_\_/ \___/|_|\__,_|  \___|_|  \___/ \_/\_/ \__,_|_|_| |_|\__, |
        //                                                                                   |___/ 
        // Separation: avoid crowding neighbours (short range repulsion)

        // The purpose of this rule is to for sheep to ensure they don't collide into each other.

        // We simply look at each sheep, and if it's within a defined small distance (say 6 pixels) of
        // another sheep move it as far away again as it already is. This is done by subtracting from a
        // vector c the displacement of each boid which is near by.

        const float c_size = 6;

        // We initialise c to zero as we want this rule to give us a vector which when added to the
        // current position moves a boid away from those near it.
        PointF c = new(0, 0);

        // collision with another sheep?
        foreach (Sheep sheep in sheepInTheflock)
        {
            if (sheep == thisSheep) continue; // excludes this sheep

            if (DistanceBetweenPoints(sheep.Position, thisSheep.Position) < c_size)
            {
                c.X -= (sheep.Position.X - thisSheep.Position.X);
                c.Y -= (sheep.Position.Y - thisSheep.Position.Y);
            }
        }

        // collision with any walls?
        foreach (PointF[] points in s_lines)
        {
            for (int i = 0; i < points.Length - 1; i++) // -1, because we're doing line "i" to "i+1"
            {
                PointF point1 = points[i];
                PointF point2 = points[i + 1];

                // touched wall? returns the closest point on the line to the sheep. We check the distance
                if (Utils.IsOnLine(point1, point2, new PointF(thisSheep.Position.X + c.X, thisSheep.Position.Y + c.Y), out PointF closest) && Utils.DistanceBetweenTwoPoints(closest, thisSheep.Position) < 10)
                {
                    // yes, need to back off from the wall
                    c.X -= 2 * (closest.X - thisSheep.Position.X);
                    c.Y -= 2 * (closest.Y - thisSheep.Position.Y);
                }
            }
        }

        return c; // how much to separate this sheep
    }

    /// <summary>
    /// Tries to provide a vector to escape the predator.
    /// </summary>
    /// <param name="sheep"></param>
    /// <param name="predator"></param>
    /// <returns></returns>
    internal static PointF EscapeTheDog(Sheep sheep, PointF predator, float softness = 10)
    {
        //      _  _   _  _        _             _     _   ____               _       _                 
        //    _| || |_| || |      / \__   _____ (_) __| | |  _ \ _ __ ___  __| | __ _| |_ ___  _ __ ___ 
        //   |_  ..  _| || |_    / _ \ \ / / _ \| |/ _` | | |_) | '__/ _ \/ _` |/ _` | __/ _ \| '__/ __|
        //   |_      _|__   _|  / ___ \ V / (_) | | (_| | |  __/| | |  __/ (_| | (_| | || (_) | |  \__ \
        //     |_||_|    |_|   /_/   \_\_/ \___/|_|\__,_| |_|   |_|  \___|\__,_|\__,_|\__\___/|_|  |___/
        //                                                                                                      
        // Escape: make them move away from the dog

        float x = sheep.Position.X - predator.X;
        float y = sheep.Position.Y - predator.Y;

        float distToPredator = (float)Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));

        return new(x / distToPredator * InverseSquare(distToPredator, softness),
                   y / distToPredator * InverseSquare(distToPredator, softness));
    }

    /// <summary>
    /// Inverse Square Function
    /// In two of the rules, Separation and Escape, nearby objects are prioritized higher than
    /// those further away.This prioritization is described by an inverse square function.
    /// This function, seen in Formula (3.3), is referred to as inv throughout the text.
    /// </summary>
    /// <param name="x">is the distance between the objects</param>
    /// <param name="s">s is a softness factor that slows down the rapid decrease of the function value | s = 1 for Separation and s = 10 for Escape.</param>
    /// <returns></returns>
    internal static float InverseSquare(float x, float s)
    {
        float e = 0.0000f; // is a small value used to avoid division by zero, when x = 0.
        return (float)Math.Pow(x / s + e, -2);
    }

    /// <summary>
    /// Rule 3: Boids try to match velocity with near boids.
    /// </summary>
    /// <param name="thisSheep"></param>
    /// <returns></returns>
    internal PointF MatchVelocityOfNearbySheep(Sheep thisSheep)
    {
        //      _  _  _____      _    _ _               _                     _       _     _                          
        //    _| || ||___ /     / \  | (_) __ _ _ __   | |_ ___    _ __   ___(_) __ _| |__ | |__   ___  _   _ _ __ ___ 
        //   |_  ..  _||_ \    / _ \ | | |/ _` | '_ \  | __/ _ \  | '_ \ / _ \ |/ _` | '_ \| '_ \ / _ \| | | | '__/ __|
        //   |_      _|__) |  / ___ \| | | (_| | | | | | || (_) | | | | |  __/ | (_| | | | | |_) | (_) | |_| | |  \__ \
        //     |_||_||____/  /_/   \_\_|_|\__, |_| |_|  \__\___/  |_| |_|\___|_|\__, |_| |_|_.__/ \___/ \__,_|_|  |___/
        //                                |___/                                 |___/                                  
        // Alignment: steer towards average heading of neighbours

        // This is similar to Rule 1, however instead of averaging the positions of the other boids
        // we average the velocities. We calculate a 'perceived velocity', pvJ, then add a small portion
        // (about an eighth) to the boid's current velocity.

        // This is similar to Rule 1, however instead of averaging the positions of the other boids
        // we average the velocities. We calculate a 'perceived velocity', pvJ, then add a small portion
        // (about an eighth) to the boid's current velocity.

        PointF c = new(0, 0);
        int countSheep = 0;
        foreach (Sheep sheep in sheepInTheflock)
        {
            if (sheep == thisSheep) continue; // excludes this sheep

            /* The alignment rule is calculated for each sheep s. Each sheep si within a radius of
                50 pixels has a velocity siv that contributes equally to the final rule vector. The size
                of the rule vector is determined by the velocity of all nearby sheep N. The vector is
                directed in the average direction of the nearby sheep. The rule vector is calculated
                with the function .
            */

            if (DistanceBetweenPoints(sheep.Position, thisSheep.Position) > c_sheepCloseEnoughToBeAMass) continue;

            ++countSheep;

            c.X += sheep.Velocity.X;
            c.Y += sheep.Velocity.Y;
        }

        if (countSheep > 0)
        {

            c.X /= countSheep;
            c.Y /= countSheep;
        }

        return new PointF((c.X - thisSheep.Velocity.X) / 8, (c.Y - thisSheep.Velocity.Y) / 8);
    }
    #endregion

    /// <summary>
    /// Moves and draws the flock of sheep.
    /// </summary>
    internal Bitmap? MoveFlock()
    {
        int score = 0;

        // move them all, using Reynolds swarm mathematics
        foreach (Sheep s in sheepInTheflock)
        {
            s.Move();
            if (s_sheepPenScoringZone.Contains(s.Position)) ++score;
        }

        Bitmap image = new(SheepPenWidthPX, SheepPenHeightPX);

        using Graphics graphics = Graphics.FromImage(image);
        graphics.Clear(Color.Green);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        graphics.FillRectangle(s_hatchBrushForScoringZone, s_sheepPenScoringZone);

        using Pen brownPenForDrawingFences = new(Color.Brown, 4);

        foreach (PointF[] points in s_lines) graphics.DrawLines(brownPenForDrawingFences, points);

        float x = 0;
        float y = 0;


        // draw each sheep
        foreach (Sheep sheep in sheepInTheflock)
        {
            sheep.Draw(graphics, c_sheepColour);
            x += sheep.Position.X;
            y += sheep.Position.Y;
        }

        // calculate centre of ALL sheep (known as center of mass)
        PointF centerOfMass = new(x / sheepInTheflock.Count, y / sheepInTheflock.Count);

        DrawXatCenterOfMass(graphics, centerOfMass);
        DrawCircleAroundCenterOfMass(graphics, centerOfMass);

        graphics.DrawString($"Score {score}", new Font("Arial", 8), Brushes.White, 0, 0);
        graphics.Flush();

        return image;
    }

    /// <summary>
    /// Debugging requires us to know where the center of mass is. So we draw a red "X".
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="centerOfMass"></param>
    private static void DrawXatCenterOfMass(Graphics graphics, PointF centerOfMass)
    {
        // x marks the spot for center of mass
        graphics.DrawLine(Pens.Red, centerOfMass.X - 4, centerOfMass.Y - 4, centerOfMass.X + 4, centerOfMass.Y + 4);
        graphics.DrawLine(Pens.Red, centerOfMass.X - 4, centerOfMass.Y + 4, centerOfMass.X + 4, centerOfMass.Y - 4);
    }

    /// <summary>
    /// Having a circle helps us know where the center of mass ends.
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="centerOfMass"></param>
    private static void DrawCircleAroundCenterOfMass(Graphics graphics, PointF centerOfMass)
    {
        // draw circle around the CoM
        using Pen penDashedLineAroundCentreOfMass = new(Color.FromArgb(50, 255, 50, 50));

        penDashedLineAroundCentreOfMass.DashStyle = DashStyle.Dash;
        graphics.DrawEllipse(penDashedLineAroundCentreOfMass, new RectangleF(x: centerOfMass.X - 300f,
                             y: centerOfMass.Y - 300f,
                             width: (float)300f * 2f,
                             height: (float)300f * 2f));
    }

}