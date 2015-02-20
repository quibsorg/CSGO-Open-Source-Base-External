using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using D2D = System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using D3D = Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using RWPM;
using System.Security.Cryptography;

namespace DirectOverlay
{
    public partial class Form1 : Form
    {
        //Based off of Luciz's D3D overlay
        #region Pointless Stuff
        private Margins marg;
        private static Random random = new Random((int)DateTime.Now.Ticks);//thanks to McAden
        private string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }
        //this is used to specify the boundaries of the transparent area
        internal struct Margins
        {
            public int Left, Right, Top, Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]

        private static extern UInt32 GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]

        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]

        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        public const int GWL_EXSTYLE = -20;

        public const int WS_EX_LAYERED = 0x80000;

        public const int WS_EX_TRANSPARENT = 0x20;

        public const int LWA_ALPHA = 0x2;

        public const int LWA_COLORKEY = 0x1;

        [DllImport("dwmapi.dll")]
        static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMargins);

        private D3D.Device device = null;

        //D3D Drawings
        private static D3D.Line line;
        private static D3D.Font font;


        public static string GetUniqueKey(int maxSize)
        {
            char[] chars = new char[62];
            chars =
            "abcdefghijklmnopqrstuvwxyzåäöABCDEFGHIJKLMNOPQRSTUVWXYZÅÄÖ1234567890АаБбВвГгДдЕеӘәЖжЗзИиЙйКкЛлМмНнОоÖöПпПпрСсТтуФфХхҺһҺһЧч'ШшьЭэԚԜԝ".ToCharArray();
            byte[] data = new byte[1];
            RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
            crypto.GetNonZeroBytes(data);
            data = new byte[maxSize];
            crypto.GetNonZeroBytes(data);
            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }

        public Form1()
        {

            InitializeComponent();
            this.Text = GetUniqueKey(20);
            //Make the window's border completely transparant
            SetWindowLong(this.Handle, GWL_EXSTYLE,
                    (IntPtr)(GetWindowLong(this.Handle, GWL_EXSTYLE) ^ WS_EX_LAYERED ^ WS_EX_TRANSPARENT));

            //Set the Alpha on the Whole Window to 255 (solid)
            SetLayeredWindowAttributes(this.Handle, 0, 255, LWA_ALPHA);

            //Init DirectX
            //This initializes the DirectX device. It needs to be done once.
            //The alpha channel in the backbuffer is critical.
            D3D.PresentParameters presentParameters = new D3D.PresentParameters();
            presentParameters.Windowed = true;
            presentParameters.SwapEffect = D3D.SwapEffect.Discard;
            presentParameters.BackBufferFormat = D3D.Format.A8R8G8B8;

            this.device = new D3D.Device(0, D3D.DeviceType.Hardware, this.Handle,
            D3D.CreateFlags.HardwareVertexProcessing, presentParameters);

            line = new D3D.Line(this.device);
            font = new D3D.Font(device, new System.Drawing.Font("Tahoma", 9, FontStyle.Regular));

            Thread dx = new Thread(new ThreadStart(this.dxThread));
            dx.IsBackground = true;
            dx.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //Create a margin (the whole form)
            marg.Left = 0;
            marg.Top = 0;
            marg.Right = this.Width;
            marg.Bottom = this.Height;

            //Expand the Aero Glass Effect Border to the WHOLE form.
            // since we have already had the border invisible we now
            // have a completely invisible window - apart from the DirectX
            // renders NOT in black.
            DwmExtendFrameIntoClientArea(this.Handle, ref marg);
        }
        #endregion
        #region Drawing Logic
        public static void _ShadowText(string text, Point Position, Color color)
        {
            font.DrawText(null, text, new Point(Position.X + 1, Position.Y + 1), Color.Black);
            font.DrawText(null, text, Position, color);
        }

        float D3DXToRadian(float x)
        {
            return x * (float)(Math.PI / 180.0F);
        }


        public static void DrawLine(float x1, float y1, float x2, float y2, float w, Color Color)
        {
            Vector2[] vLine = new Vector2[2] { new Vector2(x1, y1), new Vector2(x2, y2) };
            line.GlLines = true;
            line.Antialias = false;
            line.Width = w;
            line.Begin();
            line.Draw(vLine, Color.ToArgb());
            line.End();
        }
        void Circle(int X, int Y, int radius, int numSides, Color Color)
        {
            float Step = (float)(Math.PI * 2.0 / numSides);
            int Count = 0;
            for (float a = 0; a < Math.PI * 2.0; a += Step)
            {
                float X1 = (float)(radius * Math.Cos(a) + X);
                float Y1 = (float)(radius * Math.Sin(a) + Y);
                float X2 = (float)(radius * Math.Cos(a + Step) + X);
                float Y2 = (float)(radius * Math.Sin(a + Step) + Y);
                if (Count != 0)
                {
                    DrawLine(X1, Y1, X2, Y2, 1, Color);
                    //_ShadowText("Count: " + Count /*+ " Coords: " + X1 + ", " + Y1 + ", " + X2 + ", " + Y2*/, new Point((int)X1, (int)Y1), Color.Red);
                    //MessageBox.Show("Count: " + Count + " Coords: " + X1 + ", " + Y1 + ", " + X2 + ", " + Y2);
                }
                Count += 2;
            }
        }
        static Color SetTransparency(int A, Color color)
        {
            return Color.FromArgb(A, color.R, color.G, color.B);
        }

        public static void DrawFilledBox(float x, float y, float w, float h, System.Drawing.Color Color)
        {
            Vector2[] vLine = new Vector2[2];

            line.GlLines = true;
            line.Antialias = false;
            line.Width = 1;

            vLine[0].X = x + w / 2;
            vLine[0].Y = y;
            vLine[1].X = x + w / 2;
            vLine[1].Y = y + h;

            line.Begin();
            line.Draw(vLine, Color.ToArgb());
            line.End();
        }

        public static void DrawTransparentBox(float x, float y, float w, float h, int transparency, System.Drawing.Color Color)
        {
            Vector2[] vLine = new Vector2[2];

            line.GlLines = true;
            line.Antialias = false;
            line.Width = w;

            vLine[0].X = x + w / 2;
            vLine[0].Y = y;
            vLine[1].X = x + w / 2;
            vLine[1].Y = y + h;
            Color halfTransparent = SetTransparency(transparency, Color);
            line.Begin();
            line.Draw(vLine, halfTransparent.ToArgb());
            line.End();
        }

        public static void DrawBox(float x, float y, float w, float h, float px, System.Drawing.Color Color)
        {
            DrawFilledBox(x, y + h, w, px, Color);
            DrawFilledBox(x - px, y, px, h, Color);
            DrawFilledBox(x, y - px, w, px, Color);
            DrawFilledBox(x + w, y, px, h, Color);
        }
        #endregion
        ProcM _m = new ProcM("csgo");
        #region _vec3
        public struct _vec3
        {
            public float x;
            public float y;
            public float z;

            public float dot(_vec3 dot)
            {
                return (x * dot.x + y * dot.y + z * dot.z);
            }
            public float distance(_vec3 l, _vec3 e)
            {
                float dist;
                dist = (float)Math.Sqrt(Math.Pow(l.x - e.x, 2) + Math.Pow(l.y - e.y, 2) + Math.Pow(l.z - e.z, 2));
                return dist;
            }
        }


        double Distance(_vec3 point1, _vec3 point2)
        {
            double distance = Math.Sqrt(((int)point1.x - (int)point2.x) * ((int)point1.x - (int)point2.x) +
                ((int)point1.y - (int)point2.y) * ((int)point1.y - (int)point2.y) +
                ((int)point1.z - (int)point2.z) * ((int)point1.z - (int)point2.z));
            distance = Math.Round(distance, 3);
            return distance;
        }


        #endregion
        #region More pointless stuff
        // ... { GLOBAL HOOK }
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x100;

        private LowLevelKeyboardProc _proc = hookProc;

        private static IntPtr hhook = IntPtr.Zero;

        public void SetHook()
        {
            IntPtr hInstance = LoadLibrary("User32");
            hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hInstance, 0);
        }

        public static void UnHook()
        {
            UnhookWindowsHookEx(hhook);
        }
        #endregion
        public static bool esp = true;
        const int pBaseOff = 0xA6C90C;
        const int entList = 0x4A0F014;
        float[] ViewMatrix = new float[16];
        _vec3 W2SNPos = new _vec3();
        const int teamoff = 0xF0;
        const int PosOffset = 0x134;
        const int hOff = 0xFC;
        const int boneMatrix = 0xA78;
        const int eLoopAmt = 0x10;
        const int pAngs = 0x13DC;
        int _client;
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        _vec3 GetentPosition(int entptr)
        {
            int ent = _m.rdInt(_client + entList + (entptr * 0x10));
            return _m.rdVector(ent + PosOffset);
        }
        _vec3 GetBonePos(int target, int bone)
        {
            int bMatrix = _m.rdInt(target + boneMatrix);
            _vec3 vec = new _vec3();
            vec.x = _m.rdFloat(bMatrix + (0x30 * bone) + 0xC);
            vec.y = _m.rdFloat(bMatrix + (0x30 * bone) + 0x1C);
            vec.z = _m.rdFloat(bMatrix + (0x30 * bone) + 0x2C);
            return vec;
        }
        _vec3 GetPlayerPosition()
        {
            int ent = _m.rdInt(_client + pBaseOff);
            return _m.rdVector(ent + PosOffset);
        }

        _vec3 GetEyeAngles()
        {
            int LocalPlayer = _m.rdInt(_client + pBaseOff);

            return _m.rdVector(LocalPlayer + 0x239C);

        }


        bool w2scn(_vec3 from, _vec3 to)
        {
            float w = 0.0f;

            to.x = ViewMatrix[0] * from.x + ViewMatrix[1] * from.y + ViewMatrix[2] * from.z + ViewMatrix[3];
            to.y = ViewMatrix[4] * from.x + ViewMatrix[5] * from.y + ViewMatrix[6] * from.z + ViewMatrix[7];
            w = ViewMatrix[12] * from.x + ViewMatrix[13] * from.y + ViewMatrix[14] * from.z + ViewMatrix[15];

            if (w < 0.01f)
                return false;

            float invw = 1.0f / w;
            to.x *= invw;
            to.y *= invw;

            int width = Width;
            int height = Height;

            float x = width / 2;
            float y = height / 2;

            x += 0.5f * to.x * width + 0.5f;
            y -= 0.5f * to.y * height + 0.5f;

            to.x = x;
            to.y = y;

            W2SNPos.x = to.x;
            W2SNPos.y = to.y;
            return true;
        }
        public static IntPtr hookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            bool ValidKeyDown = false;
            if (vkCode == Keys.Insert.GetHashCode())
                ValidKeyDown = true;
            if (code >= 0 && wParam == (IntPtr)WM_KEYDOWN && ValidKeyDown)
            {
                if (vkCode == Keys.Insert.GetHashCode())
                {
                    esp = !esp;
                }
                return (IntPtr)1;
            }
            else
                return CallNextHookEx(hhook, code, (int)wParam, lParam);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetHook();
        }

        private void Form1_Closing(object sender, EventArgs e)
        {
            UnHook();
            this.device.Dispose();
        }

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        int GetLocalPlayer()
        {
            int LocalPlayer = _m.rdInt(_client + pBaseOff);
            return LocalPlayer;
        }

        #region  RADAR THINGS
        void FillRGB(float x, float y, float w, float h, int r, int g, int b, int a)
        {
            Vector2[] vLine = new Vector2[2];

            line.Width = w;

            vLine[0].X = x + w / 2;
            vLine[0].Y = y;
            vLine[1].X = x + w / 2;
            vLine[1].Y = y + h;

            Color c = Color.FromArgb(a, r, g, b);

            line.Begin();
            line.Draw(vLine, c);
            line.End();
        }


        void DrawLine(float x, float y, float xx, float yy, int r, int g, int b, int a)
        {
            Vector2[] dLine = new Vector2[2];

            line.Width = 1;

            dLine[0].X = x;
            dLine[0].Y = y;

            dLine[1].X = xx;
            dLine[1].Y = yy;

            Color c = Color.FromArgb(a, r, g, b);

            line.Draw(dLine, c);

        }

        void GradientFunc(int x, int y, int w, int h, int r, int g, int b, int a)
        {
            int iColorr, iColorg, iColorb;
            for (int i = 1; i < h; i++)
            {
                iColorr = (int)((float)i / h * r);
                iColorg = (int)((float)i / h * g);
                iColorb = (int)((float)i / h * b);
                FillRGB(x, y + i, w, 1, r - iColorr, g - iColorg, b - iColorb, a);
            }
        }

        void DrawBox(float x, float y, float width, float height, float px, int r, int g, int b, int a)
        {
            Vector2[] points = new Vector2[5];

            points[0] = new Vector2(x, y);
            points[1] = new Vector2(x + width, y);
            points[2] = new Vector2(x + width, y + height);
            points[3] = new Vector2(x, y + height);
            points[4] = new Vector2(x, y);

            line.Width = px;
            Color c = Color.FromArgb(a, r, g, b);
            line.Draw(points, c);
        }


        void DrawGUIBox(float x, float y, float w, float h, int r, int g, int b, int a, int rr, int gg, int bb, int aa)
        {
            DrawBox(x, y, w, h, 1, r, g, b, a);        // Body outline 
            FillRGB(x, y, w, h, rr, gg, bb, a);        // Body color 
        }

        #endregion

        int EnemyPosToRadar(_vec3 pos, int player, int yOff)
        {
            float r_1, r_2;
            float x_1, y_1;

            _vec3 LocalPlayerPos;
            LocalPlayerPos = _m.rdVector(GetLocalPlayer() + PosOffset);

            r_1 = -(pos.y - LocalPlayerPos.y); //Get the player's position in relation to my own, being as I'm the center of the radar.
            r_2 = pos.x - LocalPlayerPos.x;

            _vec3 eyeAngles = GetEyeAngles();
            float Yaw = eyeAngles.y - 90.0f;

            float yawToRadian = D3DXToRadian(Yaw);
            x_1 = (float)(r_2 * (float)Math.Cos((double)(yawToRadian)) - r_1 * Math.Sin((double)(yawToRadian))) / 20; // Try to calculate their X & Y on the radar, using their position relative to me, and also taking my yaw into account
            y_1 = (float)(r_2 * (float)Math.Sin((double)(yawToRadian)) + r_1 * Math.Cos((double)(yawToRadian))) / 20;

            //x_1 *= 2;
            x_1 += 123; // this adds half the width of the radar to the x pos

            y_1 += 599; // + half height.

            //Draws this code so I can see the numbers incase they're just retarded

            /* Clamps */
            if (x_1 < 25)
            {
                x_1 = 25;
            }
            if (x_1 > 220)
            {
                x_1 = 220;
            }

            if (y_1 > 697)
            {
                y_1 = 697;
            }
            if (y_1 < 502)
            {
                y_1 = 502;
            }
            /* Clamps */

            /* Colors the box in relation to their team #*/
            y_1 += yOff;
            int enemyTeam = _m.rdInt(player + teamoff);
            int localTeam = _m.rdInt(GetLocalPlayer() + teamoff);
            if (enemyTeam == localTeam)
            {
                if (pos.z - LocalPlayerPos.z > 75)
                {
                    FillRGB(x_1, y_1, 5, 5, 255, 0, 255, 255); // Purple
                }
                else if (pos.z - LocalPlayerPos.z < -75)
                {
                    FillRGB(x_1, y_1, 5, 5, 7, 223, 255, 255);// Light Blue
                }
                else
                {
                    FillRGB(x_1, y_1, 5, 5, 0, 255, 0, 255);// Green
                }
            }

            if (enemyTeam != localTeam)
            {
                if (pos.z - LocalPlayerPos.z > 75)
                {
                    FillRGB(x_1, y_1, 5, 5, 255, 255, 0, 255); // Yellow
                }
                else if (pos.z - LocalPlayerPos.z < -75)
                {
                    FillRGB(x_1, y_1, 5, 5, 255, 111, 0, 255);// Orange
                }
                else
                {
                    FillRGB(x_1, y_1, 5, 5, 255, 0, 0, 255); // Red
                }
            }
            return 0;
        }

        const int fuckingshitoffsetfuck = 8;
        private void dxThread()
        {
            _m.StartProcess();
            _client = _m.DllImageAddress("client.dll");
            while (true)
            {
                //Time how long this shit takes
                System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
                s.Start();

                //Do d3d shit
                device.Clear(D3D.ClearFlags.Target, Color.FromArgb(0, 0, 0, 0), 1.0f, 0);
                device.RenderState.ZBufferEnable = false;
                device.RenderState.Lighting = false;
                device.RenderState.CullMode = D3D.Cull.None;
                device.Transform.Projection = Matrix.OrthoOffCenterLH(0, this.Width, this.Height, 0, 0, 1);
                device.BeginScene();

                //Local Player Reads
                int LocalPlayer = _m.rdInt(_client + pBaseOff);
                int LocalPlayerHealth = _m.rdInt(LocalPlayer + hOff);
                int LocalTeam = _m.rdInt(LocalPlayer + teamoff);
                _vec3 playerPos = GetPlayerPosition();
                //Recoil crosshair, fuck toggles
                if (GetAsyncKeyState(0x001) != 0)
                {
                    _vec3 punchAngs = _m.rdVector(LocalPlayer + pAngs);
                    int crX = this.Width / 2, crY = this.Height / 2;
                    int dy = this.Height / 90;
                    int dx = this.Width / 90;

                    int drX = crX - (int)(dx * (punchAngs.y)) + fuckingshitoffsetfuck;
                    int drY = crY + (int)(dy * (punchAngs.x)) + fuckingshitoffsetfuck;

                    DrawLine(drX - 6, drY, drX + 6, drY, 1, Color.Red);
                    DrawLine(drX, drY - 6, drX, drY + 6, 1, Color.Red);
                }


                //Draw the radar
               
                int yOff = this.Height - 600-200;
                DrawGUIBox(25, yOff + 501, 200, 200, 255, 255, 255, 200, 0, 0, 0, 0);
                //GradientFunc(25, yOff + 500, 200, 200, 10, 10, 80, 250);
                FillRGB(25, yOff + 500, 200, 200, 90, 90, 90, 255);
                DrawLine(25, yOff + 500, 125, yOff + 600, 150, 150, 150, 255);
                DrawLine(225, yOff + 500, 125, yOff + 600, 150, 150, 150, 255);
                DrawLine(125, yOff + 600, 125, yOff + 700, 150, 150, 150, 255);
                DrawLine(25, yOff + 600, 225, yOff + 600, 150, 150, 150, 255);

                FillRGB(124, yOff + 600, 3, 3, 255, 255, 255, 255);
                
                //ViewMatrix Read
                //Mind you this is terribly inefficient
                for (int j = 0; j < 16; j++)
                    ViewMatrix[j] = _m.rdFloat(_client + 0x4A04564 + (j * 0x4));

                //Entity Loop
                for (int i = 0; i < 64; i++)
                {
                    int ent = _m.rdInt(_client + entList + (i * 0x10));

                    if (ent == 0 || ent == LocalPlayer)
                        continue;
                    int entHealth = _m.rdInt(ent + hOff);
                    if (entHealth == 0)
                        continue;
                    int entTeam = _m.rdInt(ent + teamoff);
                    _vec3 entPos = GetentPosition(i);
                    _vec3 test = new _vec3();

                    //Draw them on the Radar
                    EnemyPosToRadar(entPos, ent, yOff);
                    //Radar Done

                    if (w2scn(entPos, test))
                    {
                        Point pos = new Point((int)W2SNPos.x, (int)W2SNPos.y);

                        Color col;
                        if (entTeam != LocalTeam)
                        {
                            col = Color.Red;
                        }
                        else
                        {
                            col = Color.Green;
                        }
                        if (esp)
                        {
                            _ShadowText("" + entHealth, pos, col);
                            _vec3 head = GetBonePos(ent, 10);
                            if (w2scn(head, test))
                            {
                                float height = W2SNPos.y - pos.Y;
                                float headPosx = W2SNPos.x;
                                float headPosy = W2SNPos.y;
                                //Box
                                DrawLine(pos.X - (height / 4), headPosy, pos.X - (height / 4), pos.Y, 1, col);
                                DrawLine(pos.X + (height / 4), headPosy, pos.X + (height / 4), pos.Y, 1, col);
                                DrawLine(pos.X - (height / 4), headPosy, pos.X + (height / 4), headPosy, 1, col);
                                DrawLine(pos.X - (height / 4), pos.Y, pos.X + (height / 4), pos.Y, 1, col);

                                //Line
                                //DrawLine(pos.X, headPosy, pos.X, pos.Y,1, col);

                            }
                        }
                    }

                }


                int TextX = 15;

                //Draw a little box thing
                DrawTransparentBox(0, 0, 250, 20, 175, Color.Black);
                //Hack title, fuck you
                _ShadowText("CSGO Overlay by Jumbo and Mambda", new Point(1, 0), Color.DeepSkyBlue);
                //Little lines to look cool and shit
                DrawLine(250, 0, 250, 20, 1, Color.DeepSkyBlue);
                DrawLine(0, 20, 250, 20, 1, Color.DeepSkyBlue);

                s.Stop();
                //See how long that shit took
                _ShadowText("" + s.ElapsedMilliseconds + "ms", new Point(220, 0), Color.DeepSkyBlue);
                device.EndScene();
                device.Present();
                //End that d3d shit
            }
        }
    }
}
