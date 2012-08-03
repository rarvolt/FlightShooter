using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Flight_Shooter
{
    class Bullet
    {
        public static List<Bullet> list = new List<Bullet>();
        public static double lastBulletTime;
        public Vector3 position;
        public Quaternion rotation;
    }
}
