using Memory;
using System.Runtime.InteropServices;
using System.Threading;

namespace SomethingAwesomeProject
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys vKey);

        #region Offsets

        string Local = "ac_client.exe+0x18AC00"; //module+LocalPlayeraddress
        string EntityList = "ac_client.exe+0x18AC04";
        string Health = ",0xEC";
        string X = ",0x28";
        string Y = ",0x2C";
        string Z = ",0x30";
        string ViewY = ",0x38";
        string ViewX = ",0x34";

        #endregion

        Mem memory = new Mem();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            int PID = memory.GetProcIdFromName("ac_client"); // Check if AssaultCube is running
            if (PID > 0)
            {
                memory.OpenProcess(PID); // If running open process create a thread which runs aimbot
                Thread AB = new Thread(Aimbot) { IsBackground = true }; // run in background and close current thread
                AB.Start();
            }
        }

        void Aimbot()
        {
            while (true) // run in infinite loop, run forever
            {
                if (GetAsyncKeyState(Keys.XButton2) < 0)
                {
                    // Gets the information of the local player
                    var LocalPlayer = new Player
                    {
                        X = memory.ReadFloat(Local + X),
                        Y = memory.ReadFloat(Local + Y),
                        Z = memory.ReadFloat(Local + Z)
                    };
                    var Players = GetEnemyPlayers(LocalPlayer);

                    Players = Players.OrderBy(e => e.Magnitude).ToList();

                    if (Players.Count != 0)
                    {
                        AimLock(LocalPlayer, Players[0]);
                    }
                }

                Thread.Sleep(2); // 2ms delay does not run instantly
            }
        }
        // Gets the information of the local player


        // Calculates the magnitude between the local player and enemy player
        float CalculateMagnitude(Player LocalPlayer, Player Enemy)
        {
            float magnitude;
            magnitude = (float)Math.Sqrt(Math.Pow(Enemy.X - LocalPlayer.X, 2) +
                            Math.Pow(Enemy.Y - LocalPlayer.Y, 2) +
                            Math.Pow(Enemy.Z - LocalPlayer.Z, 2));

            return magnitude;
        }

        void AimLock(Player LocalPlayer, Player Enemy)
        {
            float deltaX = Enemy.X - LocalPlayer.X;
            float deltaY = Enemy.Y - LocalPlayer.Y;
            float deltaZ = Enemy.Z - LocalPlayer.Z;
            //return in radians -> convert to degrees, rotation is offset by 90 degrees
            float newViewX = (float)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI) + 90;
            double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            float newViewY = (float)(Math.Atan2(deltaZ, distance) * 180 / Math.PI);

            memory.WriteMemory(Local + ViewX, "float", newViewX.ToString());
            memory.WriteMemory(Local + ViewY, "float", newViewY.ToString());
        }

        List<Player> GetEnemyPlayers(Player LocalPlayer)
        {
            var enemyPlayers = new List<Player>();

            for (int i = 0; i < 20; i++)
            {   
                // i times 4 bytes, so it goes exactly 4 bytes between entity also becomes hexadecimal
                var CurrentEntityStr = EntityList + ",0x" + (i * 0x4).ToString("X");

                var Player = new Player
                {
                    X = memory.ReadFloat(CurrentEntityStr + X),
                    Y = memory.ReadFloat(CurrentEntityStr + Y),
                    Z = memory.ReadFloat(CurrentEntityStr + Z),
                    Health = memory.ReadInt(CurrentEntityStr + Health)

                };

                Player.Magnitude = CalculateMagnitude(LocalPlayer, Player);

                // Check player is alive
                if (Player.Health > 0 && Player.Health < 101)
                {
                    enemyPlayers.Add(Player);
                }
            }

            return enemyPlayers;
        }

    }
}
