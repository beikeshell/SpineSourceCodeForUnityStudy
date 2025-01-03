/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated September 24, 2021. Replaces all prior versions.
 *
 * Copyright (c) 2013-2021, Esoteric Software LLC
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
 * THE SPINE RUNTIMES ARE PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES,
 * BUSINESS INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THE SPINE RUNTIMES, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using System;

namespace Spine {
	/// <summary>>An attachment with vertices that are transformed by one or more bones and can be deformed by a slot's
	/// <see cref="Slot.Deform"/>.</summary>
	public abstract class VertexAttachment : Attachment {
		static int nextID = 0;
		static readonly Object nextIdLock = new Object();

		internal readonly int id;
		internal VertexAttachment timelineAttachment;
		internal int[] bones;
		internal float[] vertices;
		internal int worldVerticesLength;

		/// <summary>Gets a unique ID for this attachment.</summary>
		public int Id { get { return id; } }
		public int[] Bones { get { return bones; } set { bones = value; } }
		public float[] Vertices { get { return vertices; } set { vertices = value; } }
		public int WorldVerticesLength { get { return worldVerticesLength; } set { worldVerticesLength = value; } }
		///<summary>Timelines for the timeline attachment are also applied to this attachment.
		/// May be null if no attachment-specific timelines should be applied.</summary>
		public VertexAttachment TimelineAttachment { get { return timelineAttachment; } set { timelineAttachment = value; } }

		public VertexAttachment (string name)
			: base(name) {

			lock (VertexAttachment.nextIdLock) {
				id = VertexAttachment.nextID++;
			}
			timelineAttachment = this;
		}

		/// <summary>Copy constructor.</summary>
		public VertexAttachment (VertexAttachment other)
			: base(other) {

			lock (VertexAttachment.nextIdLock) {
				id = VertexAttachment.nextID++;
			}
			timelineAttachment = other.timelineAttachment;
			if (other.bones != null) {
				bones = new int[other.bones.Length];
				Array.Copy(other.bones, 0, bones, 0, bones.Length);
			} else
				bones = null;

			if (other.vertices != null) {
				vertices = new float[other.vertices.Length];
				Array.Copy(other.vertices, 0, vertices, 0, vertices.Length);
			} else
				vertices = null;

			worldVerticesLength = other.worldVerticesLength;
		}

		public void ComputeWorldVertices (Slot slot, float[] worldVertices) {
			ComputeWorldVertices(slot, 0, worldVerticesLength, worldVertices, 0);
		}

		/// <summary>
		/// Transforms the attachment's local <see cref="Vertices"/> to world coordinates. If the slot's <see cref="Slot.Deform"/> is
		/// not empty, it is used to deform the vertices.
		/// ComputeWorldVertices 是 VertexAttachment 类中的一个方法，用于将附件的本地顶点坐标（Vertices）转换为世界坐标。它考虑了骨骼的变换和插槽（Slot）的变形（Deform），从而生成最终的世界坐标。
		/// 这个方法的主要目的是在骨骼动画中，将与附件相关的顶点从局部坐标空间转换到全局坐标空间，以便用于渲染或其他操作。
		/// <para />
		/// See <a href="http://esotericsoftware.com/spine-runtime-skeletons#World-transforms">World transforms</a> in the Spine
		/// Runtimes Guide.
		/// </summary>
		/// <param name="slot">附件所属的 Slot 对象，包含骨骼信息（如变形数据和骨骼的世界变换）</param>
		/// <param name="start">
		///		The index of the first <see cref="Vertices"/> value to transform. Each vertex has 2 values, x and y.
		///		本地顶点数组（Vertices）中开始计算的索引。顶点是二维的，每个顶点由 x 和 y 两个值组成。
		///	</param>
		/// <param name="count">
		///		The number of world vertex values to output. Must be less than or equal to <see cref="WorldVerticesLength"/> - start.
		///		要计算的顶点数量（以浮点值为单位）。注意，这个值是输出的世界坐标值的数量（x, y 成对）
		///	</param>
		/// <param name="worldVertices">
		///		The output world vertices. Must have a length greater than or equal to <paramref name="offset"/> + <paramref name="count"/>
		///		输出的世界坐标数组，函数会将计算结果写入到这个数组中。
		/// .</param>
		/// <param name="offset">The <paramref name="worldVertices"/>
		///		index to begin writing values.
		///		在 worldVertices 中开始写入的索引。
		/// </param>
		/// <param name="stride">
		///		The number of <paramref name="worldVertices"/> entries between the value pairs written.
		///		输出数组中每对顶点（x, y）之间的间隔，默认为 2。
		///
		/// 1. 处理顶点的两种情况
		/// 顶点数据的来源有两种：
		///		未绑定到骨骼的顶点（bones == null）：顶点直接由一个骨骼变换。
		///		绑定到多个骨骼的顶点（bones != null）：顶点受多个骨骼的加权变换影响。
		/// </param>
		public virtual void ComputeWorldVertices (Slot slot, int start, int count, float[] worldVertices, int offset, int stride = 2) {
			count = offset + (count >> 1) * stride;
			ExposedList<float> deformArray = slot.deform;
			float[] vertices = this.vertices;
			int[] bones = this.bones;
			if (bones == null) {
				if (deformArray.Count > 0) vertices = deformArray.Items;
				Bone bone = slot.bone;
				float x = bone.worldX, y = bone.worldY;
				float a = bone.a, b = bone.b, c = bone.c, d = bone.d;
				for (int vv = start, w = offset; w < count; vv += 2, w += stride) {
					float vx = vertices[vv], vy = vertices[vv + 1];
					worldVertices[w] = vx * a + vy * b + x;
					worldVertices[w + 1] = vx * c + vy * d + y;
				}
				return;
			}
			
			// 骨骼绑定信息（bones）:
			// bones 是一个数组，存储了每个顶点的骨骼绑定信息。
			// 每个顶点的骨骼绑定由以下数据组成：
			//		第一个值是绑定的骨骼数量 n。
			//		接下来的 n 个值是骨骼索引。
			// 跳过不需要的顶点:
			//		根据 start 参数，跳过一些顶点的骨骼绑定数据。
			int v = 0, skip = 0;
			for (int i = 0; i < start; i += 2) {
				int n = bones[v];
				v += n + 1;
				skip += n;
			}
			Bone[] skeletonBones = slot.bone.skeleton.bones.Items;
			// 没有变形数据
			// 如果插槽没有变形数据（deformArray.Count == 0），直接使用原始顶点数据计算：
			// 遍历每个顶点绑定的骨骼。
			// 对每个骨骼：
			//		使用骨骼的世界变换矩阵将顶点转换为世界坐标。
			//		根据权重（weight）对结果进行加权累加。
			if (deformArray.Count == 0) {
				for (int w = offset, b = skip * 3; w < count; w += stride) {
					float wx = 0, wy = 0;
					int n = bones[v++];
					n += v;
					for (; v < n; v++, b += 3) {
						Bone bone = skeletonBones[bones[v]];
						float vx = vertices[b], vy = vertices[b + 1], weight = vertices[b + 2];
						wx += (vx * bone.a + vy * bone.b + bone.worldX) * weight;
						wy += (vx * bone.c + vy * bone.d + bone.worldY) * weight;
					}
					worldVertices[w] = wx;
					worldVertices[w + 1] = wy;
				}
			} else {
				// 有变形数据
				// 如果插槽有变形数据（deformArray.Count > 0），则在计算前将变形数据应用到顶点：
				// 变形数据是一个数组，与顶点一一对应。
				// 每个顶点的坐标加上对应的变形数据后，再进行加权变换。
				float[] deform = deformArray.Items;
				for (int w = offset, b = skip * 3, f = skip << 1; w < count; w += stride) {
					float wx = 0, wy = 0;
					int n = bones[v++];
					n += v;
					for (; v < n; v++, b += 3, f += 2) {
						Bone bone = skeletonBones[bones[v]];
						float vx = vertices[b] + deform[f], vy = vertices[b + 1] + deform[f + 1], weight = vertices[b + 2];
						wx += (vx * bone.a + vy * bone.b + bone.worldX) * weight;
						wy += (vx * bone.c + vy * bone.d + bone.worldY) * weight;
					}
					worldVertices[w] = wx;
					worldVertices[w + 1] = wy;
				}
			}
		}
	}
}
