using System;
using System.Collections.Generic;
using System.Linq;
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
        struct Bullet
        {
            public Vector3 position;
            public Quaternion rotation;
        }

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GraphicsDevice device;
        Effect effect;
        Texture2D sceneryTexture;
        Texture2D bulletTexture;
        Texture2D[] skyboxTextures;
        Model xwingModel;
        Model targetModel;
        Model skyboxModel;
        VertexBuffer cityVertexBuffer;
        SamplerState _clamp;
        int fps = 0;
        int tmpFps = 0;
        int counter = 0;

        double lastBulletTime = 0;
        float gameSpeed = 1.0f;
        const int minTargets = 50;
        int maxTargets = 0;
        int[,] floorPlan;
        int[] buildingHeights = new int[] { 0, 2, 2, 6, 5, 4 };
        Vector3 lightDirection = new Vector3(3, -2, 5);
        Vector3 xwingPosition = new Vector3(2, 6, -2);
        Vector3 cameraPosition;
        Vector3 cameraUpDirection;
        Quaternion xwingRotation = Quaternion.Identity;
        Quaternion cameraRotation = Quaternion.Identity;
        List<BoundingSphere> targetList = new List<BoundingSphere>();
        List<Bullet> bulletList = new List<Bullet>();

        BoundingBox[] buildingBoundingBoxes;
        BoundingBox completeCityBox;

        Matrix viewMatrix;
        Matrix projectionMatrix;

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

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            device = graphics.GraphicsDevice;

            effect = Content.Load<Effect>("effects");
            sceneryTexture = Content.Load<Texture2D>("texturemap");
            bulletTexture = Content.Load<Texture2D>("bullet");
            xwingModel = LoadModel("xwing");
            targetModel = LoadModel("target");
            skyboxModel = LoadModel("skybox", out skyboxTextures);

            LoadFloorPlan();
            SetUpVertices();
            SetUpBoundingBoxes();

            Random random = new Random();
            maxTargets = random.Next(minTargets, (int)Math.Sqrt(floorPlan.GetLength(0) * floorPlan.GetLength(1)) * floorPlan.GetLength(1) / 2);
            AddTargets();
        }

        private void LoadFloorPlan()
        {
            Random random = new Random();

            floorPlan = new int[random.Next(100) + 20, random.Next(100) + 20];

            for (int x = 0; x < floorPlan.GetLength(0); x++)
            {
                for (int y = 0; y < floorPlan.GetLength(1); y++)
                {
                    if (random.Next(0, 5) == 0)
                        floorPlan[x, y] = 1;

                    if (x > 0 && x < 3 && y > 0 && y < 3)
                        floorPlan[x, y] = 0;

                    if (x == 0 ||
                        x == floorPlan.GetLength(0) - 1 ||
                        y == 0 ||
                        y == floorPlan.GetLength(1) - 1)
                    {
                        floorPlan[x, y] = 1;
                    }
                }
            }


            int differentBuildings = buildingHeights.Length - 1;
            for (int x = 0; x < floorPlan.GetLength(0); x++)
                for (int y = 0; y < floorPlan.GetLength(1); y++)
                    if (floorPlan[x, y] == 1)
                        floorPlan[x, y] = random.Next(differentBuildings) + 1;
        }

        private void SetUpVertices()
        {
            int differentBuildings = buildingHeights.Length - 1;
            float imagesInTexture = 1 + differentBuildings * 2;

            int cityWidth = floorPlan.GetLength(0);
            int cityLength = floorPlan.GetLength(1);


            List<VertexPositionNormalTexture> verticesList = new List<VertexPositionNormalTexture>();
            for (int x = 0; x < cityWidth; x++)
            {
                for (int z = 0; z < cityLength; z++)
                {
                    int currentbuilding = floorPlan[x, z];

                    //floor or ceiling
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z), new Vector3(0, 1, 0), new Vector2(currentbuilding * 2 / imagesInTexture, 1)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 1, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z), new Vector3(0, 1, 0), new Vector2((currentbuilding * 2 + 1) / imagesInTexture, 1)));

                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 1, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 1, 0), new Vector2((currentbuilding * 2 + 1) / imagesInTexture, 0)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z), new Vector3(0, 1, 0), new Vector2((currentbuilding * 2 + 1) / imagesInTexture, 1)));

                    if (currentbuilding != 0)
                    {
                        //front wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 1)));

                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));

                        //back wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));

                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));

                        //left wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z - 1), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));

                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));

                        //right wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z - 1), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z - 1), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 1)));

                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z - 1), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                    }
                }
            }

            cityVertexBuffer = new VertexBuffer(device, VertexPositionNormalTexture.VertexDeclaration, verticesList.Count, BufferUsage.WriteOnly);

            cityVertexBuffer.SetData<VertexPositionNormalTexture>(verticesList.ToArray());
        }

        private void SetUpBoundingBoxes()
        {
            int cityWidth = floorPlan.GetLength(0);
            int cityLength = floorPlan.GetLength(1);
            int buildingType = 0;
            int buildingHeight = 0;

            List<BoundingBox> bbList = new List<BoundingBox>();
            for (int x = 0; x < cityWidth; x++)
            {
                for (int z = 0; z < cityLength; z++)
                {
                    buildingType = floorPlan[x, z];
                    if (buildingType != 0)
                    {
                        buildingHeight = buildingHeights[buildingType];
                        Vector3[] buildingPoints = new Vector3[2];
                        buildingPoints[0] = new Vector3(x, 0, -z);
                        buildingPoints[1] = new Vector3(x + 1, buildingHeight, -z - 1);
                        BoundingBox buildingBox = BoundingBox.CreateFromPoints(buildingPoints);
                        bbList.Add(buildingBox);
                    }
                }
            }
            buildingBoundingBoxes = bbList.ToArray();

            Vector3[] boundaryPoints = new Vector3[2];
            boundaryPoints[0] = new Vector3(0, 0, 0);
            boundaryPoints[1] = new Vector3(cityWidth, 20, -cityLength);
            completeCityBox = BoundingBox.CreateFromPoints(boundaryPoints);
        }

        private void AddTargets()
        {
            int cityWidth = floorPlan.GetLength(0);
            int cityLength = floorPlan.GetLength(1);

            Random random = new Random();


            while (targetList.Count < maxTargets)
            {
                int x = random.Next(cityWidth);
                int z = -random.Next(cityLength);
                float y = (float)random.Next(2000) / 1000f + 1;
                float radius = (float)random.Next(1000) / 1000f * 0.2f + 0.01f;

                BoundingSphere newTarget = new BoundingSphere(new Vector3(x, y, z), radius);

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

            ProcessKeyboard(gameTime);

            float moveSpeed = gameTime.ElapsedGameTime.Milliseconds / 500.0f * gameSpeed;
            MoveForward(ref xwingPosition, xwingRotation, moveSpeed);

            BoundingSphere xwingSphere = new BoundingSphere(xwingPosition, 0.04f);
            if (CheckCollision(xwingSphere) != CollisionType.None)
            {
                Random random = new Random();
                xwingPosition = new Vector3(random.Next(1, floorPlan.GetLength(0)), 6, -random.Next(1, floorPlan.GetLength(1)));
                xwingRotation = Quaternion.Identity;
                gameSpeed /= 1.1f;
            }

            UpdateCamera();
            UpdateBulletPositions(moveSpeed);

            Window.Title =
                "Flight Shooter by RARvolt  [gameSpeed = " + gameSpeed.ToString() +
                "; targets = " + maxTargets.ToString() +
                "; cityWidth = " + floorPlan.GetLength(0).ToString() +
                "; cityLength = " + floorPlan.GetLength(1).ToString() + "]  " +
                fps.ToString() + "fps";

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
            cameraRotation = Quaternion.Lerp(cameraRotation, xwingRotation, 0.08f);

            Vector3 campos = new Vector3(0, 0.1f, 0.6f);
            campos = Vector3.Transform(campos, Matrix.CreateFromQuaternion(cameraRotation));
            campos += xwingPosition;

            Vector3 camup = new Vector3(0, 1, 0);
            camup = Vector3.Transform(camup, Matrix.CreateFromQuaternion(cameraRotation));

            viewMatrix = Matrix.CreateLookAt(campos, xwingPosition, camup);
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.2f, 500.0f);

            cameraPosition = campos;
            cameraUpDirection = camup;
        }

        private void UpdateBulletPositions(float moveSpeed)
        {
            Bullet currentBullet;
            BoundingSphere bulletSphere;
            CollisionType colType;

            for (int i = 0; i < bulletList.Count; i++)
            {
                currentBullet = bulletList[i];
                MoveForward(ref currentBullet.position, currentBullet.rotation, moveSpeed * 2.0f);
                bulletList[i] = currentBullet;

                bulletSphere = new BoundingSphere(currentBullet.position, 0.05f);
                colType = CheckCollision(bulletSphere);
                if (colType != CollisionType.None)
                {
                    bulletList.RemoveAt(i);
                    i--;

                    if (colType == CollisionType.Target)
                        gameSpeed *= 1.05f;
                }
            }
        }

        private void ProcessKeyboard(GameTime gameTime)
        {
            float leftRightRot = 0;
            float upDownRot = 0;
            float horizontalRot = 0;

            float turningSpeed = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
            turningSpeed *= 1.6f * gameSpeed;

            KeyboardState keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.Escape))
                this.Exit();
            if (keys.IsKeyDown(Keys.Right))
                leftRightRot += turningSpeed;
            if (keys.IsKeyDown(Keys.Left))
                leftRightRot -= turningSpeed;
            if (keys.IsKeyDown(Keys.Down))
                upDownRot += turningSpeed;
            if (keys.IsKeyDown(Keys.Up))
                upDownRot -= turningSpeed;
            if (keys.IsKeyDown(Keys.Q))
                horizontalRot += turningSpeed / 1.5f;
            if (keys.IsKeyDown(Keys.E))
                horizontalRot -= turningSpeed / 1.5f;
            if (keys.IsKeyDown(Keys.Space))
            {
                double currentTime = gameTime.TotalGameTime.TotalMilliseconds;
                if (currentTime - lastBulletTime > 100)
                {
                    Bullet newBullet = new Bullet();
                    newBullet.position = xwingPosition;
                    newBullet.rotation = xwingRotation;
                    bulletList.Add(newBullet);

                    lastBulletTime = currentTime;
                }
            }
            if (keys.IsKeyDown(Keys.F2))
            {

            }

            Quaternion additionalRot =
                Quaternion.CreateFromAxisAngle(new Vector3(0, 0, -1), leftRightRot) *
                Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), upDownRot) *
                Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), horizontalRot);
            xwingRotation *= additionalRot;
        }

        private void MoveForward(ref Vector3 position, Quaternion rotationQuat, float speed)
        {
            position += Vector3.Transform(new Vector3(0, 0, -1), rotationQuat) * speed;
        }

        private CollisionType CheckCollision(BoundingSphere sphere)
        {
            for (int i = 0; i < buildingBoundingBoxes.Length; i++)
                if (buildingBoundingBoxes[i].Contains(sphere) != ContainmentType.Disjoint)
                    return CollisionType.Building;

            if (completeCityBox.Contains(sphere) != ContainmentType.Contains)
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
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.DarkSlateBlue, 1.0f, 0);

            DrawSkybox();
            DrawCity();
            DrawModel();
            DrawTargets();
            DrawBullets();

            tmpFps++;

            base.Draw(gameTime);
        }

        private void DrawCity()
        {
            effect.CurrentTechnique = effect.Techniques["Textured"];
            effect.Parameters["xWorld"].SetValue(Matrix.Identity);
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xTexture"].SetValue(sceneryTexture);
            effect.Parameters["xEnableLighting"].SetValue(true);
            effect.Parameters["xLightDirection"].SetValue(lightDirection);
            effect.Parameters["xAmbient"].SetValue(0.5f);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {

                pass.Apply();
                device.SetVertexBuffer(cityVertexBuffer);
                device.SamplerStates[0] = _clamp;
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, cityVertexBuffer.VertexCount / 3);
            }
        }

        private void DrawModel()
        {
            Matrix worldMatrix = Matrix.CreateScale(0.0005f, 0.0005f, 0.0005f) * Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateFromQuaternion(xwingRotation) * Matrix.CreateTranslation(xwingPosition);

            Matrix[] xwingTransforms = new Matrix[xwingModel.Bones.Count];
            xwingModel.CopyAbsoluteBoneTransformsTo(xwingTransforms);
            foreach (ModelMesh mesh in xwingModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Colored"];
                    currentEffect.Parameters["xWorld"].SetValue(xwingTransforms[mesh.ParentBone.Index] * worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xEnableLighting"].SetValue(true);
                    currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                    currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                }
                mesh.Draw();
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
            if (bulletList.Count > 0)
            {
                VertexPositionTexture[] bulletVertices = new VertexPositionTexture[bulletList.Count * 6];
                Vector3 center;
                int i = 0;
                foreach (Bullet currentBullet in bulletList)
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

                    device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, bulletVertices, 0, bulletList.Count * 2);
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
                        Matrix.CreateTranslation(xwingPosition);
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