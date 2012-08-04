using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Flight_Shooter
{
    class MouseProcessor
    {
        float leftRightRot = 0;
        float upDownRot = 0;

        public void ProcessMouse(GameTime gameTime, float gameSpeed)
        {
            MouseState mouse = Mouse.GetState();
            Vector2 rot = new Vector2();

            rot = Vector2.Lerp(new Vector2(mouse.X - Main.device.Viewport.Width / 2, mouse.Y - Main.device.Viewport.Height / 2), rot, 0.1f);

            Mouse.SetPosition(400, 300);

            leftRightRot = (rot.X / 700.0f) * gameSpeed;
            upDownRot = (rot.Y / 700.0f) * gameSpeed;

            Quaternion additionalRot =
                Quaternion.CreateFromAxisAngle(new Vector3(0, 0, -1), leftRightRot) *
                Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), upDownRot);
            Xwing.rotation *= additionalRot;

            if (mouse.LeftButton == ButtonState.Pressed)
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
        }
    }
}
