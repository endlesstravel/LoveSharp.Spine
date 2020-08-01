using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Love;
using static Love.Misc.MeshUtils;

namespace Spine
{
    class SuperArray<T>
    {
        T[] eleArray = new T[0];
        public T this[int index]
        {
            get
            {
                return eleArray[index];
            }

            set
            {
                eleArray[index] = value;
            }
        }

        public void IncreaseCapacity(int num)
        {
            if (eleArray.Length < num)
            {
                var oldArray = eleArray;
                eleArray = new T[num];
                Array.Copy(oldArray, eleArray, oldArray.Length);
            }
        }
        public T[] ToArray() => eleArray;
    }

    class PolygonBatcher
    {
        #region
        Mesh mesh;
        readonly int maxVerticesLength = 0;
        readonly int maxIndicesLength = 0;
        int verticesLength = 0;
        int indicesLength = 0;

        Texture lastTexture = null;
        bool isDrawing = false;
        int drawCalls = 0;

        public int DrawCallCount => drawCalls;

        readonly SuperArray<uint> indices = new SuperArray<uint>();

        VertexPositionColorTextureColor vertex = new VertexPositionColorTextureColor(0, 0, 0, 0, new Color(0f, 0f, 0f, 1f), new Color(0f, 0f, 0f, 1f));
        VertexPositionColorTextureColor tempVertex = new VertexPositionColorTextureColor(0, 0, 0, 0, Color.White, Color.White);
        readonly static Shader twoColorTintShader = Graphics.NewShader(@"
attribute vec4 VertexColor2;
varying vec4 color2;

vec4 position(mat4 transform_projection, vec4 vertex_position)
{
    color2 = VertexColor2;
    return transform_projection * vertex_position;
}
		",

        @"
varying vec4 color2;

vec4 effect(vec4 color, Image texture, vec2 texture_coords, vec2 screen_coords) {
	vec4 texColor = Texel(texture, texture_coords);
	float alpha = texColor.a * color.a;
	vec4 outputColor;
	outputColor.a = alpha;
	outputColor.rgb = (1.0 - texColor.rgb) * color2.rgb * alpha + texColor.rgb * color.rgb;
	return outputColor;
}
");

        #endregion

        public PolygonBatcher(int vertexCount)
        {
            mesh = Graphics.NewMesh(VertexPositionColorTextureColor.VertexFormatDescribute, vertexCount, MeshDrawMode.Trangles, SpriteBatchUsage.Dynamic);
            //mesh = Graphics.NewMesh(Love.Misc.MeshUtils.GetVertexFormat(), vertexCount, MeshDrawMode.Trangles, SpriteBatchUsage.Dynamic);
            maxVerticesLength = vertexCount;
            maxIndicesLength = vertexCount * 3;

            indices.IncreaseCapacity(this.maxIndicesLength);
            
            for (int i = 0; i < this.maxIndicesLength; i++)
            {
                indices[i] = 0;
            }
        }




        public void Begin()
        {
            if (this.isDrawing)
                throw new Exception("PolygonBatcher is already drawing. Call PolygonBatcher:stop() before calling PolygonBatcher:begin().");

            lastTexture = null;
            isDrawing = true;
            drawCalls = 0;
        }

        public void Draw(Texture texture, float[] vertices, float[] uvs, int numVertices, int[] indices, Color color, Color darkColor, IVertexEffect vertexEffect)
        {
            var numIndices = indices.Length;
            if (texture != lastTexture)
            {
                Flush();
                lastTexture = texture;
                mesh.SetTexture(texture);
            }

            if (this.verticesLength + numVertices >= this.maxVerticesLength || this.indicesLength + numIndices > this.maxIndicesLength)
            {
                Flush();

                // overflow ..... to split ...
                //if (numVertices >= this.maxVerticesLength || numIndices > this.maxIndicesLength)
                //{
                //    int lenVertices = numVertices;
                //    int lenIndices = indices.Length;

                //    // full bufer
                //    int maxVV = maxVerticesLength - 2;
                //    int maxII = maxIndicesLength - 6;

                //    var verticesA = GenCopyed(vertices, 0, maxVV);
                //    var uvsA = GenCopyed(uvs, 0, maxVV);
                //    var indicesA = GenCopyed(indices, 0, maxII);
                //    Draw(texture, verticesA, uvsA, maxVV, indicesA, color, darkColor, vertexEffect);

                //    // remain
                //    var verticesB = GenCopyed(vertices, maxVV, lenVertices - maxVV);
                //    var uvsB = GenCopyed(uvs, maxVV, lenVertices - maxVV);
                //    var indicesB = GenCopyed(indices, maxII, lenIndices - maxII);
                //    Draw(texture, verticesB, uvsB, lenVertices - maxVV, indicesB, color, darkColor, vertexEffect);
                //    return;
                //}
            }


           
            var indexStart = this.indicesLength;
            var offset = this.verticesLength;
            var indexEnd = indexStart + numIndices;
            var meshIndices = this.indices;
	        for (int i = 0; indexStart < indexEnd; i++)
            {
                meshIndices[indexStart] = (uint)(indices[i] + offset);
                indexStart = indexStart + 1;
            }

            this.indicesLength = this.indicesLength + numIndices;

            var vertexStart = this.verticesLength;
            var vertexEnd = vertexStart + numVertices;
            var vertex = this.vertex;



            if (vertexEffect != null)
            {
                for (int v = 0; vertexStart < vertexEnd; vertexStart +=1, v +=2)
                {
                    tempVertex.X = vertices[v + 0];
                    tempVertex.Y = vertices[v + 1];
                    tempVertex.U = uvs[v + 0];
                    tempVertex.V = uvs[v + 1];
                    tempVertex.Light = color;
                    tempVertex.Dark = darkColor;

                    vertexEffect.Transform(ref tempVertex);

                    vertex.X = tempVertex.X;
                    vertex.Y = tempVertex.Y;
                    vertex.U = tempVertex.U;
                    vertex.V = tempVertex.V;
                    vertex.Light = tempVertex.Light;
                    vertex.Dark = tempVertex.Dark;

                    //mesh.SetVertex(vertexStart, VertexPositionColorTextureColor.VertexInfo.GetData(new VertexPositionColorTextureColor[] { vertex }));
                    mesh.SetVertex(vertexStart, VertexPositionColorTextureColor.TransformToBytesByBuffer(ref vertex));
                    //mesh.SetVertex(vertexStart, VertexPositionColorTextureColor.TransformToBytesByCopy(ref vertex));
                }
            }
            else
            {
                for (int v = 0; vertexStart < vertexEnd; vertexStart += 1, v += 2)
                {
                    vertex.X = vertices[v + 0];
                    vertex.Y = vertices[v + 1];
                    vertex.U = uvs[v + 0];
                    vertex.V = uvs[v + 1];
                    vertex.Light = color;
                    vertex.Dark = darkColor;

                    //mesh.SetVertex(vertexStart, VertexPositionColorTextureColor.VertexInfo.GetData(new VertexPositionColorTextureColor[] { vertex }));
                    mesh.SetVertex(vertexStart, VertexPositionColorTextureColor.TransformToBytesByBuffer(ref vertex));
                    //mesh.SetVertex(vertexStart, VertexPositionColorTextureColor.TransformToBytesByCopy(ref vertex));
                }
            }


            this.verticesLength = this.verticesLength + numVertices;
        }

        T[] GenCopyed<T>(T[] src, int sourceIndex, int len)
        {
            T[] subArray = new T[len];
            Array.Copy(src, sourceIndex, subArray, 0, subArray.Length);
            return subArray;
        }

        public void Flush()
        {
            if (this.verticesLength == 0) return;
            var indicesArray = this.indices.ToArray();
            mesh.SetVertexMap(indicesArray);
            ////if (this.indicesLength < Mathf.CeilToInt(indicesArray.Length * 0.75f)) // optimization code
            //if (this.indicesLength < indicesArray.Length) // optimization code
            //{
            //    uint[] subArray = new uint[this.indicesLength];
            //    Array.Copy(indicesArray, subArray, subArray.Length);
            //    mesh.SetVertexMap(indicesArray);
            //}
            //else
            //{
            //    mesh.SetVertexMap(indicesArray);
            //}
            mesh.SetDrawRange(0, this.indicesLength);

            Graphics.SetShader(twoColorTintShader);
            Graphics.Draw(mesh);
            Graphics.SetShader();

            this.verticesLength = 0;
            this.indicesLength = 0;
            this.drawCalls++;
        }

        public void Stop()
        {
            if (this.isDrawing == false)
                throw new Exception("PolygonBatcher is not drawing. Call PolygonBatcher:begin() first.");

            if (this.verticesLength > 0)
                this.Flush();

            lastTexture = null;
            isDrawing = false;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VertexPositionColorTextureColor
    {
        [FieldOffset(0), MeshAttributeName("VertexPosition")] public float X;
        [FieldOffset(4), MeshAttributeName("VertexPosition")] public float Y;

        [FieldOffset(8), MeshAttributeName("VertexTexCoord")] public float U;
        [FieldOffset(12), MeshAttributeName("VertexTexCoord")] public float V;

        [FieldOffset(16), MeshAttributeName("VertexColor")] public byte R;
        [FieldOffset(17), MeshAttributeName("VertexColor")] public byte G;
        [FieldOffset(18), MeshAttributeName("VertexColor")] public byte B;
        [FieldOffset(19), MeshAttributeName("VertexColor")] public byte A;
        
        [FieldOffset(20), MeshAttributeName("VertexColor2")] public byte R2;
        [FieldOffset(21), MeshAttributeName("VertexColor2")] public byte G2;
        [FieldOffset(22), MeshAttributeName("VertexColor2")] public byte B2;
        [FieldOffset(23), MeshAttributeName("VertexColor2")] public byte A2;


        [FieldOffset(0)] byte b_x1;
        [FieldOffset(1)] byte b_x2;
        [FieldOffset(2)] byte b_x3;
        [FieldOffset(3)] byte b_x4;
        [FieldOffset(4)] byte b_y1;
        [FieldOffset(5)] byte b_y2;
        [FieldOffset(6)] byte b_y3;
        [FieldOffset(7)] byte b_y4;
        [FieldOffset(8)] byte b_u1;
        [FieldOffset(9)] byte b_u2;
        [FieldOffset(10)] byte b_u3;
        [FieldOffset(11)] byte b_u4;
        [FieldOffset(12)] byte b_v1;
        [FieldOffset(13)] byte b_v2;
        [FieldOffset(14)] byte b_v3;
        [FieldOffset(15)] byte b_v4;

        public readonly static MeshFormatDescribe<VertexPositionColorTextureColor> VertexFormatDescribute = MeshFormatDescribe.New<VertexPositionColorTextureColor>();


        //public static byte[] TransformToBytesByCopy(ref VertexPositionColorTextureColor vpcc)
        //{
        //    unsafe
        //    {
        //        fixed (void * pSrc = &vpcc)
        //        {
        //            fixed(void* pDest = transform_buffer)
        //            {
        //                Buffer.MemoryCopy(pSrc, pDest, transform_buffer.Length, transform_buffer.Length);
        //            }
        //        }
        //    }
        //    return transform_buffer;
        //}


        public static byte[] transform_buffer = new byte[24];
        public static byte[] TransformToBytesByBuffer(ref VertexPositionColorTextureColor vpcc)
        {
            transform_buffer[0] = vpcc.b_x1;
            transform_buffer[1] = vpcc.b_x2;
            transform_buffer[2] = vpcc.b_x3;
            transform_buffer[3] = vpcc.b_x4;

            transform_buffer[4] = vpcc.b_y1;
            transform_buffer[5] = vpcc.b_y2;
            transform_buffer[6] = vpcc.b_y3;
            transform_buffer[7] = vpcc.b_y4;

            transform_buffer[8] = vpcc.b_u1;
            transform_buffer[9] = vpcc.b_u2;
            transform_buffer[10] = vpcc.b_u3;
            transform_buffer[11] = vpcc.b_u4;

            transform_buffer[12] = vpcc.b_v1;
            transform_buffer[13] = vpcc.b_v2;
            transform_buffer[14] = vpcc.b_v3;
            transform_buffer[15] = vpcc.b_v4;

            transform_buffer[16] = vpcc.R;
            transform_buffer[17] = vpcc.G;
            transform_buffer[18] = vpcc.B;
            transform_buffer[19] = vpcc.A;

            transform_buffer[20] = vpcc.R2;
            transform_buffer[21] = vpcc.G2;
            transform_buffer[22] = vpcc.B2;
            transform_buffer[23] = vpcc.A2;

            return transform_buffer;
        }
        public static byte[] TransformToBytes(ref VertexPositionColorTextureColor vpcc)
        {
            return new byte[]
            {
                vpcc.b_x1,
                vpcc.b_x2,
                vpcc.b_x3,
                vpcc.b_x4,
                vpcc.b_y1,
                vpcc.b_y2,
                vpcc.b_y3,
                vpcc.b_y4,
                vpcc.b_u1,
                vpcc.b_u2,
                vpcc.b_u3,
                vpcc.b_u4,
                vpcc.b_v1,
                vpcc.b_v2,
                vpcc.b_v3,
                vpcc.b_v4,

                vpcc.R, vpcc.G, vpcc.B, vpcc.A,
                vpcc.R2, vpcc.G2, vpcc.B2, vpcc.A2,
            };
        }

        public Color Light
        {
            get => new Color(R, G, B, A);
            set
            {
                this.R = value.r;
                this.G = value.g;
                this.B = value.b;
                this.A = value.a;
            }
        }

        public Color Dark
        {
            get => new Color(R2, G2, B2, A2);
            set
            {
                this.R2 = value.r;
                this.G2 = value.g;
                this.B2 = value.b;
                this.A2 = value.a;
            }
        }


        public VertexPositionColorTextureColor(float x, float y, float u, float v, Color light, Color dark)
        {
            b_x1 = 0;
            b_x2 = 0;
            b_x3 = 0;
            b_x4 = 0;
            b_y1 = 0;
            b_y2 = 0;
            b_y3 = 0;
            b_y4 = 0;
            b_u1 = 0;
            b_u2 = 0;
            b_u3 = 0;
            b_u4 = 0;
            b_v1 = 0;
            b_v2 = 0;
            b_v3 = 0;
            b_v4 = 0;

            X = x;
            Y = y;
            U = u;
            V = v;
            R = light.r;
            G = light.g;
            B = light.b;
            A = light.a;
            R2 = dark.r;
            G2 = dark.g;
            B2 = dark.b;
            A2 = dark.a;
        }
    }
}
