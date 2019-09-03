using Love;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spine
{
    public class SkeletonRenderer
    {
        readonly PolygonBatcher batcher = new PolygonBatcher(3 * 500);
        bool premultipliedAlpha = false;
        SkeletonClipping clipper = new SkeletonClipping();
        IVertexEffect vertexEffect = null;

        public IVertexEffect VertexEffect
        {
            set => vertexEffect = value;
            get => vertexEffect;
        }

        static public readonly int[] QUAD_TRIANGLES = new int[] {
              0, 1, 2, 2, 3, 0
            //1, 2, 3, 3, 4, 1
        };

        public SkeletonRenderer()
        {

        }

        static float[] worldVertices = new float[10000 * 12];
        Color tmpColor = new Color(0f, 0f, 0f, 0f);
        Color tmpColor2 = new Color(0f, 0f, 0f, 0f);

        public int DrawCallCount => batcher.DrawCallCount;

        public void Draw(Skeleton skeleton)
        {
            if (this.vertexEffect != null)
            {
                this.vertexEffect.Begin(skeleton);
            }

            Graphics.GetBlendMode(out var lastLoveBlendMode, out var lastLoveAlphaBlendMode);
            Graphics.SetBlendMode(Love.BlendMode.Alpha);
            Spine.BlendMode lastBlendMode = Spine.BlendMode.Normal;
            batcher.Begin();


            var drawOrder = skeleton.DrawOrder;
            foreach (var slot in drawOrder.Items)
            {
                if (slot.Bone.Active)
                {
                    var attachment = slot.Attachment;
                    float[] vertices = worldVertices;
                    float[] uvs = null;
                    int[] indices = null;
                    Texture texture = null;
                    var color = tmpColor;
                    int numVertices = 0;

                    Color attachmentColor = Color.PowderBlue;

                    if (attachment is RegionAttachment)
                    {
                        RegionAttachment regionAttachment = (RegionAttachment)attachment;
                        AtlasRegion region = (AtlasRegion)regionAttachment.RendererObject;
                        texture = (Texture)region.page.rendererObject;
                        attachmentColor = new Color(regionAttachment.R, regionAttachment.G, regionAttachment.B, regionAttachment.A);

                        numVertices = 4;
                        regionAttachment.ComputeWorldVertices(slot.Bone, vertices, 0, 2);
                        uvs = regionAttachment.UVs;
                        indices = QUAD_TRIANGLES;
                    }
                    else if (attachment is MeshAttachment)
                    {
                        MeshAttachment meshAttachment = (MeshAttachment)attachment;
                        AtlasRegion region = (AtlasRegion)meshAttachment.RendererObject;
                        texture = (Texture)region.page.rendererObject;
                        attachmentColor = new Color(meshAttachment.R, meshAttachment.G, meshAttachment.B, meshAttachment.A);

                        numVertices = meshAttachment.WorldVerticesLength / 2;
                        meshAttachment.ComputeWorldVertices(slot, 0, meshAttachment.WorldVerticesLength, vertices, 0, 2);
                        uvs = meshAttachment.UVs;
                        indices = meshAttachment.Triangles;
                    }
                    else if (attachment is ClippingAttachment)
                    {
                        ClippingAttachment clip = (ClippingAttachment)attachment;
                        clipper.ClipStart(slot, clip);

                        continue; 
                    }
                    else
                    {
                        continue;
                    }


                    if (texture != null)
                    {
                        var slotBlendMode = slot.Data.BlendMode;
					    if (lastBlendMode != slotBlendMode)
                        {
                            batcher.Stop();
                            batcher.Begin();

                            if (slotBlendMode == Spine.BlendMode.Normal)
                                Graphics.SetBlendMode(Love.BlendMode.Alpha);
                            else if (slotBlendMode == Spine.BlendMode.Additive)
                                Graphics.SetBlendMode(Love.BlendMode.Add);
                            else if (slotBlendMode == Spine.BlendMode.Multiply)
                                Graphics.SetBlendMode(Love.BlendMode.Multiply, BlendAlphaMode.PreMultiplied);
                            else if (slotBlendMode == Spine.BlendMode.Screen)
                                Graphics.SetBlendMode(Love.BlendMode.Screen);

                            lastBlendMode = slotBlendMode;
                        }


                        var skeletonTEMP = slot.Bone.Skeleton;
                        var skeletonColor = new Color(skeletonTEMP.R, skeletonTEMP.G, skeletonTEMP.G, skeletonTEMP.A);
                        var slotColor = new Color(slot.R, slot.G, slot.B, slot.A);
                        var alpha = skeletonColor.Af * slotColor.Af * attachmentColor.Af;
                        var multiplier = alpha;
                        if (premultipliedAlpha) multiplier = 1f;

                        color.Rf = skeletonColor.Rf * slotColor.Rf * attachmentColor.Rf * alpha * multiplier;
                        color.Gf = skeletonColor.Gf * slotColor.Gf * attachmentColor.Gf * alpha * multiplier;
                        color.Bf = skeletonColor.Bf * slotColor.Bf * attachmentColor.Bf * alpha * multiplier;
                        color.Af = alpha;

                        var dark = tmpColor2;

                        if (slot.HasSecondColor)
                        {
                            dark.Rf = slot.R2;
                            dark.Gf = slot.G2;
                            dark.Bf = slot.B2;
                            dark.Af = slot.A;
                            //dark.Af = 1;
                        }
                        else
                        {
                            dark.Rf = 0;
                            dark.Gf = 0;
                            dark.Bf = 0;
                            dark.Af = 0;
                            //dark.Af = 1;
                        }
                        dark.Af = premultipliedAlpha ? 1 : 0;

                        if (this.clipper.IsClipping)
                        {
                            this.clipper.ClipTriangles(vertices, 
                                numVertices << 1, // unused ????
                                indices, indices.Length, uvs);
						    vertices = this.clipper.ClippedVertices.Items;
                            numVertices = vertices.Length / 2;
						    uvs = this.clipper.ClippedUVs.Items;
                            indices = this.clipper.ClippedTriangles.Items;
                        }

                        batcher.Draw(texture, vertices, uvs, numVertices, indices, color, dark, this.vertexEffect);
                    }


                    this.clipper.ClipEnd(slot);
                }

            } // end of foreach


            batcher.Stop();
            //Graphics.SetBlendMode(lastLoveBlendMode, lastLoveAlphaBlendMode);
            Graphics.SetBlendMode(lastLoveBlendMode);
            this.clipper.ClipEnd();
            if (this.vertexEffect != null)
            {
                this.vertexEffect.End();
            };
        }
    }
}
