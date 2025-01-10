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
	public class PathConstraintData : ConstraintData {
		// 受到路径约束的骨骼列表
		internal ExposedList<BoneData> bones = new ExposedList<BoneData>();
		// 路径约束目标插槽
		// 路径约束会将骨骼与路径绑定，而路径是通过插槽来定义的
		internal SlotData target;
		internal PositionMode positionMode;
		internal SpacingMode spacingMode;
		internal RotateMode rotateMode;
		internal float offsetRotation;
		internal float position, spacing, mixRotate, mixX, mixY;

		public PathConstraintData (string name) : base(name) {
		}

		public ExposedList<BoneData> Bones { get { return bones; } }
		public SlotData Target { get { return target; } set { target = value; } }
		public PositionMode PositionMode { get { return positionMode; } set { positionMode = value; } }
		public SpacingMode SpacingMode { get { return spacingMode; } set { spacingMode = value; } }
		public RotateMode RotateMode { get { return rotateMode; } set { rotateMode = value; } }
		// 路径约束的旋转偏移量，以角度为单位
		public float OffsetRotation { get { return offsetRotation; } set { offsetRotation = value; } }
		// 骨骼在路径上的位置，可以是固定值或百分比，取决于 positionMode。
		public float Position { get { return position; } set { position = value; } }
		// 骨骼之间的间距，可以是固定值或百分比，取决于 spacingMode。
		public float Spacing { get { return spacing; } set { spacing = value; } }
		/// <summary> A percentage (0-1) that controls the mix between the constrained and unconstrained rotation.</summary>
		public float RotateMix { get { return mixRotate; } set { mixRotate = value; } }
		/// <summary> A percentage (0-1) that controls the mix between the constrained and unconstrained translation X.</summary>
		public float MixX { get { return mixX; } set { mixX = value; } }
		/// <summary> A percentage (0-1) that controls the mix between the constrained and unconstrained translation Y.</summary>
		public float MixY { get { return mixY; } set { mixY = value; } }
	}

	public enum PositionMode {
		Fixed, // 固定值 
		Percent // 百分比
	}

	public enum SpacingMode {
		Length, // 根据路径的长度来计算间距 
		Fixed, // 使用固定间距
		Percent, // 使用路劲长度的百分比
		Proportional // 按比例分布间距
	}

	public enum RotateMode {
		Tangent, // 骨骼的旋转方向与路径的切线方向一致 
		Chain, // 骨骼按链式旋转
		ChainScale // 链式旋转，同时根据路径长度缩放骨骼
	}
}
