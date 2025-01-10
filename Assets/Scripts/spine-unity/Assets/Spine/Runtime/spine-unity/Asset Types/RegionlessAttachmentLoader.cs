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
using UnityEngine;

namespace Spine.Unity {

	// RegionlessAttachmentLoader 是一个自定义的 AttachmentLoader 类，专门用于加载 Spine 的各种附件（Attachments），
	// 但它的实现方式是为附件提供一个“空”的区域（EmptyRegion），而不是实际的纹理或图像资源。
	public class RegionlessAttachmentLoader : AttachmentLoader {

		static AtlasRegion emptyRegion;
		// EmptyRegion 是一个静态属性，用于提供一个“空的”纹理区域（AtlasRegion）。
		// 它的实现中创建了一个虚拟的 AtlasRegion 和 AtlasPage：
		// AtlasRegion 的名字是 "Empty AtlasRegion"。
		// AtlasPage 使用了一个特殊的材质（Material），其中的 Shader 是 Spine/Special/HiddenPass，并且材质的名字是 "NoRender Material"。
		// 这个设计的目的是为附件提供一个占位的区域，而不需要实际的纹理资源。
		static AtlasRegion EmptyRegion {
			get {
				if (emptyRegion == null) {
					emptyRegion = new AtlasRegion {
						name = "Empty AtlasRegion",
						page = new AtlasPage {
							name = "Empty AtlasPage",
							rendererObject = new Material(Shader.Find("Spine/Special/HiddenPass")) { name = "NoRender Material" }
						}
					};
				}
				return emptyRegion;
			}
		}

		public RegionAttachment NewRegionAttachment (Skin skin, string name, string path, Sequence sequence) {
			RegionAttachment attachment = new RegionAttachment(name) {
				Region = EmptyRegion
			};
			return attachment;
		}

		public MeshAttachment NewMeshAttachment (Skin skin, string name, string path, Sequence sequence) {
			MeshAttachment attachment = new MeshAttachment(name) {
				Region = EmptyRegion
			};
			return attachment;
		}

		public BoundingBoxAttachment NewBoundingBoxAttachment (Skin skin, string name) {
			return new BoundingBoxAttachment(name);
		}

		public PathAttachment NewPathAttachment (Skin skin, string name) {
			return new PathAttachment(name);
		}

		public PointAttachment NewPointAttachment (Skin skin, string name) {
			return new PointAttachment(name);
		}

		public ClippingAttachment NewClippingAttachment (Skin skin, string name) {
			return new ClippingAttachment(name);
		}
	}
}
