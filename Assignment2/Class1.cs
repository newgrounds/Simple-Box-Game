using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using DI = Microsoft.DirectX.DirectInput;
using System.Collections.Generic;

namespace Adam.Direct3D
{
    public class Game : System.Windows.Forms.Form
    {
        public Game()
        {
            this.Size = new Size(800,600);
            this.Text = "Follow the Object -- By Adam Gressen";
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
        }

        static void Main()
        {
            Game app = new Game();
            app.InitializeGraphics();
            app.InitializeInput();
            app.Show();
            while (app.Created)
            {
                app.Render();
                Application.DoEvents();
            }
        }

        // direct3d device
        private D3D.Device device;
        // directinput device
        private DI.Device keyDevice;
        // cube's vertex buffer
        private VertexBuffer vertices;
        // vertex buffer for all objects other than cube
        private List<VertexBuffer> environ;
        // initial cube speed
        private float cubeSpeed = 0.2F;
        // Camera rotation speed
        private float camRotSpeed = 0.01F;
        // Camera speed
        private float camSpeed = 0.02f;
        // rotation matrix for left and right movement
        Matrix right = Matrix.RotationY((float)Math.PI / 2);

        // the initial position of the cube
        Vector3 cubePos = new Vector3(0.2f, -0.5F, 0.2f);
        // the initial position of the camera
        Vector3 cameraPos = new Vector3(0, 0, -10);
        // the initial orientation of the camera
        Vector3 cameraOrientation = new Vector3(0, 0, 0);

        // tells the cube when to rotate
        Boolean rotated = false;

        // message to display at the end of the game
        String endMessage = "";

        int Red = Color.Red.ToArgb();
        int Blue = Color.Blue.ToArgb();
        int Green = Color.Green.ToArgb();


        // initialize all input devices
        public void InitializeInput()
        {
            // initialize the keyboard device
            keyDevice = new DI.Device(SystemGuid.Keyboard);
            keyDevice.SetCooperativeLevel(this, CooperativeLevelFlags.Background |
                        CooperativeLevelFlags.NonExclusive);
            keyDevice.Properties.AxisModeAbsolute = true;
            keyDevice.Acquire();
        }


        /// <summary>
        /// Subtracts the two given Vector3's and returns the resulting Vector3.
        /// </summary>
        /// <param name="vec1"> First Vector3 </param>
        /// <param name="vec2"> Second Vector3</param>
        protected Vector3 subVectors(Vector3 vec1, Vector3 vec2)
        {
            Vector3 temp = vec1;
            temp.Subtract(vec2);
            return temp;
        }


        // Handle camera manipulation based on key events
        private void KeyCamera()
        {
            // Get keyboard state
            KeyboardState state = keyDevice.GetCurrentKeyboardState();

            if (state[Key.W] || state[Key.UpArrow])
            {
                // move camera forward
                Vector3 movement = camSpeed * subVectors(cameraOrientation, cameraPos);

                cameraOrientation += movement;
                cameraPos += movement;
            }
            else if (state[Key.S] || state[Key.DownArrow])
            {
                // move camera backward
                Vector3 movement = camSpeed * subVectors(cameraOrientation, cameraPos);

                cameraOrientation += -movement;
                cameraPos += -movement;
            }
            if (state[Key.A] || state[Key.LeftArrow])
            {
                // rotated camera orientation
                Vector3 tempOrient = cameraOrientation;
                tempOrient.TransformCoordinate(right);
                // rotated camera position
                Vector3 tempCam = cameraPos;
                tempCam.TransformCoordinate(right);

                // move camera left
                Vector3 movement = camSpeed * subVectors(tempOrient, tempCam);

                cameraOrientation += -movement;
                cameraPos += -movement;
            }
            else if (state[Key.D] || state[Key.RightArrow])
            {
                // rotated camera orientation
                Vector3 tempOrient = cameraOrientation;
                tempOrient.TransformCoordinate(right);
                // rotated camera position
                Vector3 tempCam = cameraPos;
                tempCam.TransformCoordinate(right);

                // move camera right
                Vector3 movement = camSpeed * subVectors(tempOrient, tempCam);

                cameraOrientation += movement;
                cameraPos += movement;
            }
            if (state[Key.Q])
            {
                // rotate camera left
                Matrix rotY = Matrix.RotationY(-camRotSpeed);
                cameraOrientation.TransformCoordinate(rotY);
            }
            else if (state[Key.E])
            {
                // rotate camera right
                Matrix rotY = Matrix.RotationY(camRotSpeed);
                cameraOrientation.TransformCoordinate(rotY);
            }
        }

        protected bool InitializeGraphics()
        {
            PresentParameters pres = new PresentParameters();
            pres.Windowed = true;
            pres.SwapEffect = SwapEffect.Discard;

            // create direct3d device
            device = new D3D.Device(0, D3D.DeviceType.Hardware, this,
              CreateFlags.SoftwareVertexProcessing, pres);
            device.DeviceReset += new EventHandler(HandleResetEvent);

            // create the environmental objects
            environ = CreateEnvironment(device);
            // create the cube
            vertices = CreateVertexBuffer(device);
            device.VertexFormat = CustomVertex.PositionNormalColored.Format;

            return true;
        }

        
        /// <summary>
        /// Given a box dimenations and position of its front right buttom corner, creates a set of 
        /// inside the inpot vertex buffer that can be used for drawing triangles that builds up the box.
        /// </summary>
        /// <param name="vertexBuffer"> Vertex Buffer that points will be written on </param>
        /// <param name="width"> width of the box</param>
        /// <param name="height"> height of the box </param>
        /// <param name="depth"> depth of the box</param>
        /// <param name="location"> location of the front right buttom  corner of the box</param>
        public void boxByTriangles(VertexBuffer vertexBuffer, float width, float height, float depth, Vector3 location)
        {
            CustomVertex.PositionNormalColored[] verts =
             (CustomVertex.PositionNormalColored[])vertexBuffer.Lock(0, 0);

            // Normal Vector in X direction, used for lighting left side of the box
            Vector3 xNormal = new Vector3(1, 0, 0);
            // Normal Vector in -X direction, used for lighting right side of the box
            Vector3 nXNormal = new Vector3(-1, 0, 0);
            // Normal Vector in Y direction, used for lighting top side of the box
            Vector3 YNormal = new Vector3(0, 1, 0);
            // Normal Vector in -Y direction, used for lighting bottom side of the box
            Vector3 nYNormal = new Vector3(0, -1, 0);
            // Normal Vector in Z direction, used for lighting rear side of the box
            Vector3 zNormal = new Vector3(0, 0, 1);
            // Normal Vector in -Z direction, used for lighting front side of the box
            Vector3 nZNormal = new Vector3(0, 0, -1);

            //Front Side (Zplane) - T1
            int i = 0;
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, 0, 0),
              nZNormal,
              Blue);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, height, 0),
              nZNormal,
              Blue);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, 0, 0),
              nZNormal,
              Blue);
            //Front Side (Zplane) - T2
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, 0, 0),
              nZNormal,
              Blue);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, height, 0),
              nZNormal,
              Blue);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, height, 0),
              nZNormal,
              Blue);

            //Left Side (Xplane) - T1
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, 0, 0),
              nXNormal,
           Green);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, 0, depth),
              nXNormal,
              Green);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, height, 0),
              nXNormal,
              Green);
            //Left Side (X plane) - T2
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, height, depth),
              nXNormal,
              Green);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, height, 0),
              nXNormal,
              Green);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, 0, depth),
              nXNormal,
              Green);

            //Bottom Side (Y plane) - T1
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, 0, 0),
              nYNormal,
              Red);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, 0, 0),
              nYNormal,
              Red);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, 0, depth),
              nYNormal,
              Red);
            //Bottom Side (Y plane) - T2
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, 0, depth),
              nYNormal,
              Red);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, 0, depth),
              nYNormal,
              Red);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, 0, 0),
              nYNormal,
              Red);

            //Right Side (Xplane) - T1
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, 0, 0),
              xNormal,
              Green);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, height, 0),
              xNormal,
              Green);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, 0, depth),
              xNormal,
              Green);
            //Right Side (X plane) - T2
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, height, depth),
              xNormal,
              Green);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, 0, depth),
              xNormal,
              Green);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, height, 0),
              xNormal,
              Green);


            //Back Side (Zplane) - T1

            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, 0, depth),
              zNormal,
              Blue);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, 0, depth),
              zNormal,
              Blue);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, height, depth),
              zNormal,
              Blue);
            //Front Side (Zplane) - T2
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, 0, depth),
              zNormal,
              Blue);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, height, depth),
              zNormal,
              Blue);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, height, depth),
              zNormal,
              Blue);

            //Top Side (Y plane) - T1
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, height, 0),
              YNormal,
              Red);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, height, depth),
              YNormal,
              Red);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, height, 0),
              YNormal,
              Red);
            //Bottom Side (Y plane) - T2
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, height, depth),
              YNormal,
              Red);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(width, height, 0),
              YNormal,
              Red);
            verts[i++] = new CustomVertex.PositionNormalColored(
              location + new Vector3(0, height, depth),
              YNormal,
              Red);

            if (rotated)
            {
                Matrix rotM = Matrix.RotationY((float)Math.PI / 2);
                for (int v = 0; v < verts.Length; v++)
                {
                    Vector3 pos = verts[v].Position - location;
                    pos.TransformCoordinate(rotM);
                    pos += location;
                    verts[v].Position = pos;
                }
            }

            vertexBuffer.Unlock();
        }

        // create a VertexBuffer to store the cube
        protected VertexBuffer CreateVertexBuffer(D3D.Device device)
        {
            try
            {
                VertexBuffer buf = new VertexBuffer(
                    typeof(CustomVertex.PositionNormalColored),
                    36, device, 0,
                    CustomVertex.PositionNormalColored.Format, Pool.Default);
                boxByTriangles(buf, 1, 1, 1, cubePos);
                return buf;
            }
            catch (System.NullReferenceException e)
            {
                return null;
            }
        }


        // handle camera
        protected void SetupMatrices()
        {
            // check to see which keys are being pressed
            KeyCamera();

            // set the view to the left-handed look at matrix
            device.Transform.View =
            Matrix.LookAtLH(cameraPos, cameraOrientation, new Vector3(0, 1, 0));

            device.Transform.Projection =
              Matrix.PerspectiveFovLH((float)Math.PI / 4.0F,
                1.0F, 0.0001F, 50.0F);
        }


        // Handle cube movement
        protected void MoveCube()
        {
            // calculate the distance from the camera to the cube
            float dist = (float)(Math.Sqrt(Math.Pow(cubePos.X - cameraPos.X, 2) 
                + Math.Pow(cubePos.Y - cameraPos.Y, 2) 
                + Math.Pow(cubePos.Z - cameraPos.Z, 2)));

            // if distance is less than 5, increase speed
            if (dist < 5)
            {
                cubeSpeed = 0.5F;
            }
            // if distance is greater than 60, you lose
            else if (dist > 60)
            {
                endMessage = "You Lose! Thanks for Playing!";
                MessageBox.Show(endMessage);
                
                this.Close();
            }
            // if distance is greater than 15, return to normal speed
            else if (dist > 15)
            {
                cubeSpeed = 0.1F;
            }
            
            // tell cube to rotate
            if (!rotated && cubePos.Z >= 95)
            {
                rotated = true;
            }

            // move based on rotation
            if (rotated)
            {
                // along x-axis if rotated
                cubePos += new Vector3(cubeSpeed, 0, 0);
            }
            else
            {
                // along z-axis if unrotated
                cubePos += new Vector3(0, 0, cubeSpeed);
            }

            // if cube has finished movement, you win
            if (cubePos.X >= 100)
            {
                endMessage = "Congratulations! You Win!";
                MessageBox.Show(endMessage);

                this.Close();
            }
            
            // create the cube
            vertices = CreateVertexBuffer(device);
        }


        // Create lighting
        protected void SetupLights()
        {
            device.Lights[0].Diffuse = Color.White; // Color of the light
            device.Lights[0].Type = LightType.Directional;
            device.Lights[0].Direction = new Vector3(-1, -1, 3);
            device.Lights[0].Update();
            device.Lights[0].Enabled = true;
            device.RenderState.Ambient = Color.FromArgb(0x40, 0x40, 0x40);
        }


        // Render the scene
        protected void Render()
        {
            // Clear the back buffer
            device.Clear(ClearFlags.Target, Color.Bisque, 1.0F, 0);
            // Ready Direct3D to begin drawing
            device.BeginScene();
            // Set the Matrices
            SetupMatrices();
            // Set the lights
            SetupLights();

            // Handle cube movement
            MoveCube();

            try
            {
                // Draw all other objects
                foreach (VertexBuffer v in environ)
                {
                    device.SetStreamSource(0, v, 0);
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);
                }

                // Draw the cube
                device.SetStreamSource(0, vertices, 0);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);

                // Indicate to Direct3D that we’re done drawing
                device.EndScene();
                // Copy the back buffer to the display
                device.Present();
            }
            catch (System.NullReferenceException)
            {

            }
        }


        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Game
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(500, 600);
            this.Name = "Game";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Game_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Game_FormClosed);
            this.ResumeLayout(false);

        }


        // this solves the Invalid Call Exception resulting from form resizing
        private void HandleResetEvent(object caller, EventArgs args)
        {
            //device.RenderState.FillMode = FillMode.WireFrame;
            //device.RenderState.CullMode = Cull.None;
            InitializeGraphics();
            InitializeInput();
        }


        // Create the ancillary objects to show that cube and camera are moving
        protected List<VertexBuffer> CreateEnvironment(D3D.Device device)
        {
            List<VertexBuffer> env = new List<VertexBuffer>();
            Vector3 start = new Vector3(3, 0, 10);
            Vector3 nextStart = new Vector3(3, 0, 100);

            // objects along right side before rotation
            for (int i = 0; i < 9; i++)
            {
                VertexBuffer buf = new VertexBuffer(
                  typeof(CustomVertex.PositionNormalColored),
                  36, device, 0,
                  CustomVertex.PositionNormalColored.Format, Pool.Default);
                boxByTriangles(buf, 1, 1, 1, new Vector3(start.X, start.Y, start.Z + (i * 10)));
                env.Add(buf);
            }

            // objects along left side before rotation
            for (int j = 0; j < 10; j++)
            {
                VertexBuffer buf = new VertexBuffer(
                  typeof(CustomVertex.PositionNormalColored),
                  36, device, 0,
                  CustomVertex.PositionNormalColored.Format, Pool.Default);
                boxByTriangles(buf, 1, 1, 1, new Vector3(start.X-6, start.Y, start.Z + (j * 10)));
                env.Add(buf);
            }

            // objects along right side following rotation
            for (int k = 0; k < 10; k++)
            {
                VertexBuffer buf = new VertexBuffer(
                  typeof(CustomVertex.PositionNormalColored),
                  36, device, 0,
                  CustomVertex.PositionNormalColored.Format, Pool.Default);
                boxByTriangles(buf, 1, 1, 1, new Vector3(nextStart.X + (k * 10), nextStart.Y, nextStart.Z));
                env.Add(buf);
            }

            // objects along left side following rotation
            for (int l = 0; l < 10; l++)
            {
                VertexBuffer buf = new VertexBuffer(
                  typeof(CustomVertex.PositionNormalColored),
                  36, device, 0,
                  CustomVertex.PositionNormalColored.Format, Pool.Default);
                boxByTriangles(buf, 1, 1, 1, new Vector3(nextStart.X - 6 + (l * 10), nextStart.Y, nextStart.Z - 10));
                env.Add(buf);
            }

            return env;
        }

        private void Game_FormClosed(object sender, FormClosedEventArgs e)
        {
            MessageBox.Show(endMessage);
        }

        private void Game_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Dispose();
        }
    }
}