namespace Sheep
{
    /// <summary>
    /// 
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>
        /// Flock of sheep.
        /// </summary>
        Flock? flock;

        #region EVENT HANDLERS
        /// <summary>
        /// Constructor.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            timer1.Interval = 10;
        }

        /// <summary>
        /// Animation is frame by frame, initiated each tick.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (flock == null) throw new Exception("please initialise the flock in Onload()");

            Bitmap? image = flock.MoveFlock();

            pictureBox1.Image?.Dispose();
            pictureBox1.Image = image;
        }

        /// <summary>
        /// On form load, we create the flock.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            flock = new Flock(pictureBox1.Width, pictureBox1.Height);

            Flock.s_sheepPenScoringZone = new Rectangle(pictureBox1.Width - 100, 0, 96, 80);

            // the pen
            List<PointF> lines = new()
            {
                new PointF(pictureBox1.Width - 4, 80),
                new PointF(pictureBox1.Width - 4, 0),
                new PointF(pictureBox1.Width - 100, 0),
                new PointF(pictureBox1.Width - 100, 0),
                new PointF(pictureBox1.Width - 100, 80),
                new PointF(pictureBox1.Width - 150, 120)
            };

            Flock.s_lines.Add(lines.ToArray());

            // the start
            lines = new()
            {
                new PointF(pictureBox1.Width / 4, 0),
                new PointF(pictureBox1.Width / 4, pictureBox1.Height / 4 * 3)
            };

            Flock.s_lines.Add(lines.ToArray());

            // a restricted point
            lines = new()
            {
                new PointF(pictureBox1.Width / 2, 0),
                new PointF(pictureBox1.Width / 2, pictureBox1.Height / 4 * 1.8f),
                new PointF(pictureBox1.Width / 2 + pictureBox1.Width / 4, pictureBox1.Height / 4 * 1.8f)
            };
            Flock.s_lines.Add(lines.ToArray());

            lines = new()
            {
                new PointF(pictureBox1.Width / 2, pictureBox1.Height),
                new PointF(pictureBox1.Width / 2, pictureBox1.Height - pictureBox1.Height / 4 * 1.8f)
            };
            Flock.s_lines.Add(lines.ToArray());

            timer1.Start();
        }
        #endregion

        /// <summary>
        /// Set the predator based on the mouse position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            Flock.s_predator = new PointF(e.X, e.Y);
        }
    }
}