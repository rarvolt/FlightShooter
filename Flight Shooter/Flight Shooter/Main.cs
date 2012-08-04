using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Flight_Shooter
{
    public class Main : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        public static GraphicsDevice device;
        Effect effect;
        RenderTarget2D ss;
        Texture2D bulletTexture;
        Texture2D[] skyboxTextures;
        Model targetModel;
        Model skyboxModel;
        SamplerState _clamp;
        int fps = 0;
        int tmpFps = 0;
        int counter = 0;

        float gameSpeed = 1.0f;
        const int minTargets = 50;
        int maxTargets = 0;
        
        Vector3 cameraPosition;
        Vector3 cameraUpDirection;
        Quaternion cameraRotation = Quaternion.Identity;
        List<BoundingSphere> targetList = new List<BoundingSphere>();

        public static Vector3 lightDirection = new Vector3(3, -2, 5);
        public static Matrix viewMatrix;
        public static Matrix projectionMatrix;

        KeyboardProcessor keyboardProcessor;
        MouseProcessor mouseProcessor;
        City city;

        public static bool exit = false;
        public static bool screenShoot = false;
        public static bool fullscreenFlag = false;
        private bool fullscreen = false;

        enum CollisionType
        {
            None,
            Building,
            Boundary,
            Target
        }

        public Main()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            Console.Out.WriteLine("=== Flight Shooter by RARvolt ===");
            Console.Out.WriteLine("Initializing...");

            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            Window.Title = "Flight Shooter by RARvolt";

            _clamp = new SamplerState
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp
            };
            
            lightDirection.Normalize();

            keyboardProcessor = new KeyboardProcessor();
            mouseProcessor = new MouseProcessor();

            base.Initialize();

            Console.Out.WriteLine("Initializing done.");
        }

        protected override void LoadContent()
        {
            Console.Out.WriteLine("Loading Content...");

            spriteBatch = new SpriteBatch(GraphicsDevice);

            device = graphics.GraphicsDevice;

            ss = new RenderTarget2D(device, device.Viewport.Width, device.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);

            effect = Content.Load<Effect>("effects");
            bulletTexture = Content.Load<Texture2D>("bullet");
            Xwing.model = LoadModel("xwing");
            targetModel = LoadModel("target");
            skyboxModel = LoadModel("skybox", out skyboxTextures);

            city = new City(Content.Load<Texture2D>("texturemap"), effect);

            Random random = new Random();
            maxTargets = random.Next(minTargets, (int)Math.Sqrt(city.floorPlan.GetLength(0) * city.floorPlan.GetLength(1)) * city.floorPlan.GetLength(1) / 2);
            AddTargets();

            Console.Out.WriteLine("Loading Content done.");
        }

        private void AddTargets()
        {
            int cityWidth = city.floorPlan.GetLength(0);
            int cityLength = city.floorPlan.GetLength(1);

            Random random = new Random();
            BoundingSphere newTarget;

            while (targetList.Count < maxTargets)
            {
                int x = random.Next(cityWidth);
                int z = -random.Next(cityLength);
                float y = (float)random.Next(2000) / 1000f + 1;
                float radius = (float)random.Next(1000) / 1000f * 0.25f + 0.01f;

                newTarget = new BoundingSphere(new Vector3(x, y, z), radius);

                if (CheckCollision(newTarget) == CollisionType.Target)
                    continue;

                if (CheckCollision(newTarget) == CollisionType.None)
                    targetList.Add(newTarget);
            }
        }

        private Model LoadModel(string assetName)
        {

            Model newModel = Content.Load<Model>(assetName);
            foreach (ModelMesh mesh in newModel.Meshes)
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                    meshPart.Effect = effect.Clone();
            return newModel;
        }

        private Model LoadModel(string assetName, out Texture2D[] textures)
        {
            Model newModel = Content.Load<Model>(assetName);
            textures = new Texture2D[newModel.Meshes.Count];
            int i = 0;
            foreach (ModelMesh mesh in newModel.Meshes)
                foreach (BasicEffect currentEffect in mesh.Effects)
                    textures[i++] = currentEffect.Texture;

            foreach (ModelMesh mesh in newModel.Meshes)
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                    meshPart.Effect = effect.Clone();

            return newModel;
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();
            if (exit)
                this.Exit();

            keyboardProcessor.ProcessKeyboard(gameTime, gameSpeed);
            mouseProcessor.ProcessMouse(gameTime, gameSpeed);

            float moveSpeed = gameTime.ElapsedGameTime.Milliseconds / 500.0f * gameSpeed;
            MoveForward(ref Xwing.position, Xwing.rotation, moveSpeed);

            BoundingSphere xwingSphere = new BoundingSphere(Xwing.position, 0.04f);
            if (CheckCollision(xwingSphere) != CollisionType.None)
            {
                Random random = new Random();
                Xwing.position = new Vector3(random.Next(1, city.floorPlan.GetLength(0)), 6, -random.Next(1, city.floorPlan.GetLength(1)));
                Xwing.rotation = Quaternion.Identity;
                gameSpeed /= 1.1f;
            }

            UpdateCamera();
            UpdateBulletPositions(moveSpeed);

            Window.Title =
                "Flight Shooter by RARvolt  [gameSpeed = " + gameSpeed.ToString() +
                "; targets = " + maxTargets.ToString() +
                "; cityWidth = " + city.floorPlan.GetLength(0).ToString() +
                "; cityLength = " + city.floorPlan.GetLength(1).ToString() + "]  " +
                fps.ToString() + "fps" + (fullscreen ? " fullscreen" : "");

            if (counter >= 60)
            {
                fps = tmpFps;
                tmpFps = 0;
                counter = 0;
            }
            else
                counter++;

            base.Update(gameTime);
        }

        private void UpdateCamera()
        {
            cameraRotation = Quaternion.Lerp(cameraRotation, Xwing.rotation, 0.08f);

            Vector3 campos = new Vector3(0, 0.1f, 0.6f);
            campos = Vector3.Transform(campos, Matrix.CreateFromQuaternion(cameraRotation));
            campos += Xwing.position;

            Vector3 camup = new Vector3(0, 1, 0);
            camup = Vector3.Transform(camup, Matrix.CreateFromQuaternion(cameraRotation));

            viewMatrix = Matrix.CreateLookAt(campos, Xwing.position, camup);
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.2f, 500.0f);

            cameraPosition = campos;
            cameraUpDirection = camup;
        }

        private void UpdateBulletPositions(float moveSpeed)
        {
            Bullet currentBullet;
            BoundingSphere bulletSphere;
            CollisionType colType;

            for (int i = 0; i < Bullet.list.Count; i++)
            {
                currentBullet = Bullet.list[i];
                MoveForward(ref currentBullet.position, currentBullet.rotation, moveSpeed * 2.0f);
                Bullet.list[i] = currentBullet;

                bulletSphere = new BoundingSphere(currentBullet.position, 0.05f);
                colType = CheckCollision(bulletSphere);
                if (colType != CollisionType.None)
                {
                    Bullet.list.RemoveAt(i);
                    i--;

                    if (colType == CollisionType.Target)
                        gameSpeed *= 1.05f;
                }
            }
        }

        private void MoveForward(ref Vector3 position, Quaternion rotationQuat, float speed)
        {
            position += Vector3.Transform(new Vector3(0, 0, -1), rotationQuat) * speed;
        }

        private CollisionType CheckCollision(BoundingSphere sphere)
        {
            for (int i = 0; i < city.buildingBoundingBoxes.Length; i++)
            {

                if (city.buildingBoundingBoxes[i].Contains(sphere) != ContainmentType.Disjoint)
                    return CollisionType.Building;
            }

            if (city.completeCityBox.Contains(sphere) != ContainmentType.Contains)
                return CollisionType.Boundary;

            for (int i = 0; i < targetList.Count; i++)
            {
                if (targetList[i].Contains(sphere) != ContainmentType.Disjoint)
                {
                    targetList.RemoveAt(i);
                    i--;
                    AddTargets();

                    return CollisionType.Target;
                }
            }

            return CollisionType.None;
        }

        protected override void Draw(GameTime gameTime)
        {
            if (fullscreenFlag)
            {
                fullscreen = !fullscreen;
                fullscreenFlag = false;

                if (fullscreen)
                {
                    graphics.PreferredBackBufferWidth = device.DisplayMode.Width;
                    graphics.PreferredBackBufferHeight = device.DisplayMode.Height;
                }
                else
                {
                    graphics.PreferredBackBufferWidth = 800;
                    graphics.PreferredBackBufferHeight = 600;
                }
                graphics.ToggleFullScreen();
            }

            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.DarkSlateBlue, 1.0f, 0);
            if (screenShoot)
            {
                device.SetRenderTarget(ss);
            }

            DrawSkybox();
            city.Draw();
            Xwing.Draw();
            DrawTargets();
            DrawBullets();
            tmpFps++;

            base.Draw(gameTime);

            if (screenShoot)
            {
                device.SetRenderTarget(null);
                bool saved = false;
                int i = 0;
                while (!saved)
                {
                    if (!Directory.Exists(@"Screenshoots"))
                        Directory.CreateDirectory(@"Screenshoots");
                    string path = @"Screenshoots\ss" + i.ToString() + ".png";
                    if (!File.Exists(path))
                    {
                        FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
                        ss.SaveAsPng(fs, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
                        fs.Close();
                        saved = true;
                    }
                    else
                        i++;
                }
                screenShoot = false;
            }
        }

        private void DrawTargets()
        {
            Matrix worldMatrix;
            Matrix[] targetTransforms;
            for (int i = 0; i < targetList.Count; i++)
            {
                worldMatrix =
                    Matrix.CreateScale(targetList[i].Radius) *
                    Matrix.CreateTranslation(targetList[i].Center);
                targetTransforms = new Matrix[targetModel.Bones.Count];
                targetModel.CopyAbsoluteBoneTransformsTo(targetTransforms);
                foreach (ModelMesh modmesh in targetModel.Meshes)
                {
                    foreach (Effect currentEffect in modmesh.Effects)
                    {
                        currentEffect.CurrentTechnique = currentEffect.Techniques["Colored"];
                        currentEffect.Parameters["xWorld"].SetValue(targetTransforms[modmesh.ParentBone.Index] * worldMatrix);
                        currentEffect.Parameters["xView"].SetValue(viewMatrix);
                        currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                        currentEffect.Parameters["xEnableLighting"].SetValue(true);
                        currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                        currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                    }
                    modmesh.Draw();
                }
            }
        }

        private void DrawBullets()
        {
            if (Bullet.list.Count > 0)
            {
                VertexPositionTexture[] bulletVertices = new VertexPositionTexture[Bullet.list.Count * 6];
                Vector3 center;
                int i = 0;
                foreach (Bullet currentBullet in Bullet.list)
                {
                    center = currentBullet.position;

                    bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(1, 1));
                    bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(0, 0));
                    bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(1, 0));

                    bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(1, 1));
                    bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(0, 1));
                    bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(0, 0));
                }

                effect.CurrentTechnique = effect.Techniques["PointSprites"];
                effect.Parameters["xWorld"].SetValue(Matrix.Identity);
                effect.Parameters["xProjection"].SetValue(projectionMatrix);
                effect.Parameters["xView"].SetValue(viewMatrix);
                effect.Parameters["xCamPos"].SetValue(cameraPosition);
                effect.Parameters["xTexture"].SetValue(bulletTexture);
                effect.Parameters["xCamUp"].SetValue(cameraUpDirection);
                effect.Parameters["xPointSpriteSize"].SetValue(0.1f);

                device.BlendState = BlendState.Additive;
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, bulletVertices, 0, Bullet.list.Count * 2);
                }
                device.BlendState = BlendState.Opaque;
            }
        }

        private void DrawSkybox()
        {
            SamplerState ss = new SamplerState();
            ss.AddressU = TextureAddressMode.Clamp;
            ss.AddressV = TextureAddressMode.Clamp;
            device.SamplerStates[0] = ss;

            DepthStencilState dss = new DepthStencilState();
            dss.DepthBufferEnable = false;
            device.DepthStencilState = dss;

            Matrix[] skyboxTransforms = new Matrix[skyboxModel.Bones.Count];
            skyboxModel.CopyAbsoluteBoneTransformsTo(skyboxTransforms);

            Matrix worldMatrix;
            int i = 0;
            foreach (ModelMesh mesh in skyboxModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    worldMatrix =
                        skyboxTransforms[mesh.ParentBone.Index] *
                        Matrix.CreateTranslation(Xwing.position);
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Textured"];
                    currentEffect.Parameters["xWorld"].SetValue(worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xTexture"].SetValue(skyboxTextures[i++]);
                }
                mesh.Draw();
            }

            dss = new DepthStencilState();
            dss.DepthBufferEnable = true;
            device.DepthStencilState = dss;
        }
    }
}