using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Love;
using static Love.Misc.MeshUtils;

namespace Spine
{
    class SuperArray<T> : IEnumerable<T>
    {
        readonly List<T> list = new List<T>();

        public T this[int index]
        {
            get
            {
                if (index >= list.Count)
                    throw new IndexOutOfRangeException();

                return list[index];
            }

            set
            {
                if (index >= list.Count)
                {
                    var num = index - list.Count + 1;
                    list.AddRange(new T[num]);
                }
                list[index] = value;
            }
        }

        public void IncreaseCapacity(int num) => list.Capacity += num;

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

        public int Count => list.Count;
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
            mesh = Graphics.NewMesh(VertexPositionColorTextureColor.VertexInfo.formatList, vertexCount, MeshDrawMode.Trangles, SpriteBatchUsage.Dynamic);
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
            else if (this.verticesLength + numVertices >= this.maxVerticesLength || this.indicesLength + numIndices > this.maxIndicesLength)
            {
                Flush();
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

                    mesh.SetVertex(vertexStart, VertexPositionColorTextureColor.VertexInfo.GetData(new VertexPositionColorTextureColor[] { vertex }));
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

                    mesh.SetVertex(vertexStart, VertexPositionColorTextureColor.VertexInfo.GetData(new VertexPositionColorTextureColor[] { vertex }));
                }
            }


            this.verticesLength = this.verticesLength + numVertices;
        }

        public void Flush()
        {
            if (this.verticesLength == 0) return;

            mesh.SetVertexMap(this.indices.ToArray());
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


    public struct VertexPositionColorTextureColor
    {
        [Name("VertexPosition")]
        public float X, Y;

        [Name("VertexTexCoord")]
        public float U, V;

        [Name("VertexColor")]
        public byte R, G, B, A;

        [Name("VertexColor2")]
        public byte R2, G2, B2, A2;

        public readonly static Info<VertexPositionColorTextureColor> VertexInfo = Parse<VertexPositionColorTextureColor>();

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
