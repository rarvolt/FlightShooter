using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Flight_Shooter
{
    static class Xwing
    {
        public static Vector3 position = new Vector3(2, 6, -2);
        public static Quaternion rotation = Quaternion.Identity;
        public static Model model;

        public static void Draw()
        {
            Matrix worldMatrix = Matrix.CreateScale(0.0005f, 0.0005f, 0.0005f) * Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateFromQuaternion(Xwing.rotation) * Matrix.CreateTranslation(Xwing.position);

            Matrix[] xwingTransforms = new Matrix[Xwing.model.Bones.Count];
            Xwing.model.CopyAbsoluteBoneTransformsTo(xwingTransforms);
            foreach (ModelMesh mesh in Xwing.model.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Colored"];
                    currentEffect.Parameters["xWorld"].SetValue(xwingTransforms[mesh.ParentBone.Index] * worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(Main.viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(Main.projectionMatrix);
                    currentEffect.Parameters["xEnableLighting"].SetValue(true);
                    currentEffect.Parameters["xLightDirection"].SetValue(Main.lightDirection);
                    currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                }
                mesh.Draw();
            }
        }
    }
}
