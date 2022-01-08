using System;
using System.Collections.Generic;
using System.Linq;
using Impostors.RenderInstructions;
using UnityEngine;

namespace Impostors
{
    public static class CollectionsPool
    {
        private static readonly HashSet<Renderer> HashSetOfRenderers = new HashSet<Renderer>();
        private static readonly List<Renderer> ListOfRenderers = new List<Renderer>();
        private static readonly List<Material> ListOfMaterials = new List<Material>();
        private static LightmapData[] Lightmaps;
        private static float LightmapsTime = -1;
        private static RenderInstructionBufferBuilder RenderInstructionBufferBuilder = new RenderInstructionBufferBuilder();

        public static HashSet<Renderer> GetHashSetOfRenderers()
        {
            HashSetOfRenderers.Clear();
            return HashSetOfRenderers;
        }

        public static List<Renderer> GetListOfRenderers()
        {
            ListOfRenderers.Clear();
            return ListOfRenderers;
        }

        public static List<Material> GetListOfMaterials()
        {
            ListOfMaterials.Clear();
            return ListOfMaterials;
        }

        public static LightmapData[] GetLightmaps()
        {
            // There is no way to know when lightmaps array is changed,
            // that's why we cannot cache array for more than one frame.
            if (Math.Abs(LightmapsTime - Time.unscaledTime) > float.Epsilon)
            {
                Lightmaps = LightmapSettings.lightmaps;
                LightmapsTime = Time.unscaledTime;
            }
            return Lightmaps;
        }

        public static RenderInstructionBufferBuilder GetRenderInstructionBufferBuilder()
        {
            return RenderInstructionBufferBuilder;
        }
    }
}