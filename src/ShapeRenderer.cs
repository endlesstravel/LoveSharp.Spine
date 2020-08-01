/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated May 1, 2019. Replaces all prior versions.
 *
 * Copyright (c) 2013-2019, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY EXPRESS
 * OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN
 * NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES, BUSINESS
 * INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using Love;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spine {
	/// <summary>
	/// Batch drawing of lines and shapes that can be derrived from lines.
	///
	/// Call drawing methods in between Begin()/End()
	/// </summary>
	class ShapeRenderer {
		public ShapeRenderer() {
		}

		public void SetColor(Color color) {
            Graphics.SetColor(color);
		}

		public void Begin() {
		}

		public void Line(float x1, float y1, float x2, float y2) {
            Graphics.Line(x1, y1, x2, y2);
		}

		/** Calls {@link #circle(float, float, float, int)} by estimating the number of segments needed for a smooth circle. */
		public void Circle(float x, float y, float radius) {
			Circle(x, y, radius, Math.Max(1, (int)(6 * (float)Math.Pow(radius, 1.0f / 3.0f))));
		}

		/** Draws a circle using {@link ShapeType#Line} or {@link ShapeType#Filled}. */
		public void Circle(float x, float y, float radius, int segments) {
            Graphics.Circle(DrawMode.Line, x, y, radius, segments);
		}

		public void Triangle(float x1, float y1, float x2, float y2, float x3, float y3) {
            Graphics.Polygon(DrawMode.Line, x1, y1, x2, y2, x3, y3);
		}

		public void X(float x, float y, float len) {
			Line(x + len, y + len, x - len, y - len);
			Line(x - len, y + len, x + len, y - len);
		}

		public void Polygon(float[] polygonVertices, int offset, int count) {
			if (count< 3) throw new ArgumentException("Polygon must contain at least 3 vertices");
            float[] buffer = polygonVertices.Skip(offset).Take(count).ToArray();
            Graphics.Polygon(DrawMode.Line, buffer);
		}

		public void Rect(float x, float y, float width, float height) {
            Graphics.Rectangle(DrawMode.Line, x, y, width, height);
		}

		public void End() {
		}
	}

}
