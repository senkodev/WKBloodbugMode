using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BloodbugMode
{
    internal class BodyOutline
    {
        private const float DepthPush = 0.06f;
        private const float Width = 2f;
        private const float MaxDistance = 25f;

        private static readonly Color EdibleColor = new Color32(193, 48, 48, 255);
        private static readonly Color InReachColor = new Color32(255, 150, 150, 255);

        private static readonly Vector2[] Offsets =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(0.7f, 0.7f), new Vector2(-0.7f, 0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, -0.7f)
        };

        private class Body
        {
            public Denizen denizen;
            public Renderer[] renderers;
        }

        private static readonly List<Body> bodies = new List<Body>();

        private readonly Camera camera;
        private readonly BloodbugController bug;
        private readonly CommandBuffer buffer;
        private readonly Material edible;
        private readonly Material inReach;
        private bool shown;
        private bool attached;

        public static int Count => bodies.Count;

        private BodyOutline(Camera camera, BloodbugController bug, Shader shader)
        {
            this.camera = camera;
            this.bug = bug;
            edible = FlatMaterial(shader, EdibleColor);
            inReach = FlatMaterial(shader, InReachColor);
            buffer = new CommandBuffer { name = "Bloodbug prey outline" };
        }

        public static BodyOutline Create(Camera camera, BloodbugController bug)
        {
            // https://docs.unity3d.com/ScriptReference/Shader.Find.html
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                Plugin.Log.LogWarning("Hidden/Internal-Colored shader not found");
                return null;
            }
            TrackDead(UnityEngine.Object.FindObjectsByType<Denizen>(FindObjectsSortMode.None));

            var outline = new BodyOutline(camera, bug, shader);
            Camera.onPreRender += outline.Draw;
            return outline;
        }

        public static void Track(Denizen denizen)
        {
            if (!bodies.Exists(body => body.denizen == denizen))
            {
                bodies.Add(new Body { denizen = denizen });
            }
        }

        public static void TrackDead(IEnumerable<Denizen> denizens)
        {
            foreach (Denizen denizen in denizens)
            {
                if (denizen.dead)
                {
                    Track(denizen);
                }
            }
        }

        public static void Forget()
        {
            bodies.Clear();
        }

        public void Destroy()
        {
            Camera.onPreRender -= Draw;
            Detach();
            buffer.Release();
            UnityEngine.Object.Destroy(edible);
            UnityEngine.Object.Destroy(inReach);
        }

        public void Show(bool wanted)
        {
            shown = wanted;
            if (!wanted)
            {
                Detach();
            }
        }

        // https://docs.unity3d.com/ScriptReference/Camera-onPreRender.html
        // https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.html
        private void Draw(Camera rendering)
        {
            if (rendering != camera || !shown) return;
            buffer.Clear();

            Matrix4x4 view = camera.worldToCameraMatrix;
            Matrix4x4 projection = camera.projectionMatrix;
            var pixel = new Vector2(2f * Width / camera.pixelWidth, 2f * Width / camera.pixelHeight);
            Vector3 eyes = camera.transform.position;
            bool drawn = false;

            for (int i = bodies.Count - 1; i >= 0; i--)
            {
                Body body = bodies[i];
                if (body.denizen == null || bug.BloodLeft(body.denizen) <= 0f)
                {
                    bodies.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(body.denizen.transform.position, eyes);
                if (distance > MaxDistance || !body.denizen.gameObject.activeInHierarchy) continue;

                if (body.renderers == null)
                {
                    body.renderers = body.denizen.GetComponentsInChildren<Renderer>(true);
                }
                bool close = body.denizen == bug.Prey || distance <= BloodbugController.FeedReach;
                foreach (Renderer renderer in body.renderers)
                {
                    if (CanDraw(renderer))
                    {
                        DrawShifted(renderer, close ? inReach : edible, view, projection, pixel);
                        drawn = true;
                    }
                }
            }
            if (!drawn)
            {
                Detach();
                return;
            }
            buffer.SetViewProjectionMatrices(view, projection);

            if (!attached)
            {
                // https://docs.unity3d.com/ScriptReference/Rendering.CameraEvent.html
                camera.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, buffer);
                attached = true;
            }
        }

        private void DrawShifted(Renderer renderer, Material material, Matrix4x4 view, Matrix4x4 projection, Vector2 pixel)
        {
            Vector3 toRenderer = renderer.bounds.center - camera.transform.position;
            float distance = Mathf.Max(Vector3.Dot(toRenderer, camera.transform.forward), camera.nearClipPlane * 2f);
            float push = DepthPush * 2f * camera.nearClipPlane / (distance * distance);

            foreach (Vector2 offset in Offsets)
            {
                buffer.SetViewProjectionMatrices(view, Shift(projection, offset.x * pixel.x, offset.y * pixel.y, push));
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    buffer.DrawRenderer(renderer, material, i);
                }
            }
        }

        private static bool CanDraw(Renderer renderer)
        {
            return renderer != null
                && renderer.enabled
                && renderer.gameObject.activeInHierarchy
                && (renderer is SkinnedMeshRenderer || renderer is MeshRenderer);
        }

        private void Detach()
        {
            if (attached && camera != null)
            {
                camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, buffer);
            }
            attached = false;
        }

        // https://docs.unity3d.com/ScriptReference/Camera-projectionMatrix.html
        private static Matrix4x4 Shift(Matrix4x4 projection, float x, float y, float depth)
        {
            Vector4 w = projection.GetRow(3);
            projection.SetRow(0, projection.GetRow(0) + w * x);
            projection.SetRow(1, projection.GetRow(1) + w * y);
            projection.SetRow(2, projection.GetRow(2) + w * depth);
            return projection;
        }

        private static Material FlatMaterial(Shader shader, Color color)
        {
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, color = color };
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_Cull", (int)CullMode.Off);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            return material;
        }
    }
}
