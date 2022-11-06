using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

public static class Program
{
    public class Mesh
    {
        private readonly int m_vaoID;

        private readonly int m_vboID;
        private readonly int m_iboID;

        private readonly int m_indexCount;

        public Mesh(float[] vertices, uint[] indices)
        {
            m_vaoID = GL.GenVertexArray();

            m_vboID = GL.GenBuffer();
            m_iboID = GL.GenBuffer();

            m_indexCount = indices.Length;

            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
        }

        ~Mesh()
        {
            GL.DeleteVertexArray(m_vaoID);

            GL.DeleteBuffer(m_vboID);
            GL.DeleteBuffer(m_iboID);
        }

        public void Bind()
        {
            GL.BindVertexArray(m_vaoID);

            GL.BindBuffer(BufferTarget.ArrayBuffer, m_vboID);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_iboID);
        }

        public unsafe void Draw()
        {
            Bind();

            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(0, sizeof(Vector3) / sizeof(float), VertexAttribPointerType.Float, false, sizeof(Vector3), 0);

            GL.DrawElements(BeginMode.Triangles, m_indexCount, DrawElementsType.UnsignedInt, 0);

            GL.DisableVertexAttribArray(0);
        }
    }

    public class Shader
    {
        private readonly int m_ID;

        private readonly List<int> m_shaders;

        public Shader(string vertexShaderText, string fragmentShaderText)
        {
            m_shaders = new();

            m_ID = GL.CreateProgram();

            AddShader(vertexShaderText, ShaderType.VertexShader);
            AddShader(fragmentShaderText, ShaderType.FragmentShader);

            Compile();
        }

        ~Shader()
        {
            foreach(int shader in m_shaders)
                GL.DeleteShader(shader);

            GL.DeleteProgram(m_ID);
        }

        public void Bind() => GL.UseProgram(m_ID);

        public int AddUniform(string name) => GL.GetUniformLocation(m_ID, name);

        private void AddShader(string text, ShaderType type)
        {
            int shader = GL.CreateShader(type);

            if(shader == 0)
                throw new Exception("Shader creation failed: Could not find valid memory location when adding shader");

            GL.ShaderSource(shader, text);
            GL.CompileShader(shader);

            Console.WriteLine(GL.GetShaderInfoLog(shader));

            GL.AttachShader(m_ID, shader);
            m_shaders.Add(shader);
        }

        private void Compile()
        {
            GL.LinkProgram(m_ID);
            GL.ValidateProgram(m_ID);

            Console.WriteLine(GL.GetProgramInfoLog(m_ID));
        }
    }


    public static unsafe void Main(string[] args)
    {
        GameWindow window = new(new GameWindowSettings(), new NativeWindowSettings()
        {
            Size = new Vector2i(800, 600),
            WindowState = WindowState.Normal
        })
        {
            VSync = VSyncMode.Off
        };

        window.CenterWindow();

        const int INSTANCE_COUNT = 50000;

        GL.Enable(EnableCap.CullFace);

        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.CullFace(CullFaceMode.Back);

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.DepthClamp);

        float[] vertices = new float[]
        {
            -1, -1, -1,
             1, -1, -1,
            -1,  1, -1,
             1,  1, -1,
            -1, -1,  1,
             1, -1,  1,
            -1,  1,  1,
             1,  1,  1,
        };

        uint[] indices = new uint[]
        {
            0, 2, 3,
            3, 1, 0,
            6, 4, 5,
            7, 6, 5,
            1, 3, 7,
            7, 5, 1,
            4, 6, 2,
            0, 4, 2,
            3, 2, 6,
            6, 7, 3,
            5, 4, 0,
            0, 1, 5
        };

        Mesh cube = new(vertices, indices);

        Matrix4[] transformations = new Matrix4[INSTANCE_COUNT];

        Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView(70.0f * (MathF.PI / 180.0f), window.Size.X / (float)window.Size.Y, 0.01f, 1000f);

        Random random = new();
        for(int i = 0; i < INSTANCE_COUNT; i++)
        {
            Vector3 position = new Vector3(random.NextSingle() * 2.0f - 1.0f, random.NextSingle() * 2.0f - 1.0f, -random.NextSingle() - 0.5f) * (INSTANCE_COUNT * 0.01f);
            transformations[i] = Matrix4.CreateTranslation(position);
        }

        string   vertexShaderText = File.ReadAllText("VertexShader.glsl");
        string fragmentShaderText = File.ReadAllText("FragmentShader.glsl");

        Shader shader = new(vertexShaderText, fragmentShaderText);
        shader.Bind();

        int matrixUniformLocation = shader.AddUniform("u_transformation");

        TimeSpan frameTime = TimeSpan.FromSeconds(1.0f / 5000f);

        TimeSpan timer = TimeSpan.Zero;


        int fps = 0;
        DateTime lastTime = DateTime.Now;
        TimeSpan fpsTimeCounter = TimeSpan.Zero;
        TimeSpan updateTimer = TimeSpan.FromSeconds(1);

        float x = 0.0f;

        while(!window.IsExiting)
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan passedTime = currentTime - lastTime;
            lastTime = currentTime;

            updateTimer += passedTime;
            fpsTimeCounter += passedTime;

            bool shouldRender = false;
            while(updateTimer >= frameTime)
            {
                shouldRender = true;

                updateTimer -= frameTime;

                if(fpsTimeCounter >= TimeSpan.FromSeconds(1))
                {
                    Console.WriteLine("FPS: " + fps);
                    fpsTimeCounter = TimeSpan.Zero;
                    fps = 0;
                }
            }

            if(shouldRender)
            {
                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.Clear(ClearBufferMask.DepthBufferBit);

                DateTime currTime = DateTime.Now;
                TimeSpan timePassed = currTime - lastTime;
                lastTime = currTime;

                timer += timePassed;

                for(int i = 0; i < INSTANCE_COUNT; i++)
                {
                    Matrix4 MVP = transformations[i] * perspective;
                    GL.UniformMatrix4(matrixUniformLocation, false, ref MVP);
                    cube.Draw();
                }

                x += (float)frameTime.TotalSeconds;

                NativeWindow.ProcessWindowEvents(false);
                window.Context.SwapBuffers();

                fps++;
            }
        }
    }
}