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
using System.Collections.Generic;

namespace Spine {
	// ConstraintData 是所有约束数据的基类，表示骨骼动画中约束的基本信息。约束是骨骼动画中用来限制或影响骨骼行为的规则，比如 IK（反向动力学）约束、路径约束等
	/// <summary>The base class for all constraint datas.</summary>
	public abstract class ConstraintData {
		internal readonly string name;
		internal int order;
		internal bool skinRequired;

		public ConstraintData (string name) {
			if (name == null) throw new ArgumentNullException("name", "name cannot be null.");
			this.name = name;
		}

		// 约束名称, 该名称在skeleton中的所有同类约束中保持唯一.
		/// <summary> The constraint's name, which is unique across all constraints in the skeleton of the same type.</summary>
		public string Name { get { return name; } }

		// 表示约束的顺序（ordinal）。这个顺序决定了骨骼在调用 Skeleton.UpdateWorldTransform() 时，约束的应用顺序。
		///<summary>The ordinal of this constraint for the order a skeleton's constraints will be applied by
		/// <see cref="Skeleton.UpdateWorldTransform()"/>.</summary>
		public int Order { get { return order; } set { order = value; } }

		// 表示是否需要特定的皮肤（Skin）才能更新此约束。如果为 true，则只有当骨骼的皮肤包含此约束时，才会更新。
		///<summary>When true, <see cref="Skeleton.UpdateWorldTransform()"/> only updates this constraint if the <see cref="Skeleton.Skin"/> contains
		/// this constraint.</summary>
		///<seealso cref="Skin.Constraints"/>
		public bool SkinRequired { get { return skinRequired; } set { skinRequired = value; } }

		override public string ToString () {
			return name;
		}
	}
}
