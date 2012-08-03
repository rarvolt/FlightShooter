using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Flight_Shooter
{
    public class KeyboardProcessor
    {
        private bool F2p = false;
        private bool F11p = false;

        public void ProcessKeyboard(GameTime gameTime, float gameSpeed)
        {
            float leftRightRot = 0;
            float upDownRot = 0;
            float horizontalRot = 0;

            float turningSpeed = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
            turningSpeed *= 1.6f * gameSpeed;

            KeyboardState keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.Escape))
                Main.exit = true;
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
                if (currentTime - Bullet.lastBulletTime > 100)
                {
                    Bullet newBullet = new Bullet();
                    newBullet.position = Xwing.position;
                    newBullet.rotation = Xwing.rotation;
                    Bullet.list.Add(newBullet);

                    Bullet.lastBulletTime = currentTime;
                }
            }
            if (keys.IsKeyDown(Keys.F2) && !F2p)
            {
                F2p = true;
                Main.screenShoot = true;
            }
            if (keys.IsKeyUp(Keys.F2) && F2p)
                F2p = false;
            if (keys.IsKeyDown(Keys.F11) && !F11p)
            {
                F11p = true;
                Main.fullscreenFlag = true;
            }
            if (keys.IsKeyUp(Keys.F11) && F11p)
                F11p = false;

            Quaternion additionalRot =
                Quaternion.CreateFromAxisAngle(new Vector3(0, 0, -1), leftRightRot) *
                Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), upDownRot) *
                Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), horizontalRot);
                Xwing.rotation *= additionalRot;
        }
    }
}
