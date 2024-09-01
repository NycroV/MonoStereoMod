using Microsoft.Xna.Framework.Content;
using System.Reflection;

namespace MonoStereoMod
{
    public static class MonoStereoUtil
    {
        private static readonly MethodInfo getPath = typeof(Terraria.ModLoader.Engine.DistributionPlatform).Assembly
            .GetType("TMLContentManager")
            .GetMethod("GetPath", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(string)]);

        public static string GetPath(this ContentManager instance, string path) => (string)getPath.Invoke(instance, [path]);
    }
}
