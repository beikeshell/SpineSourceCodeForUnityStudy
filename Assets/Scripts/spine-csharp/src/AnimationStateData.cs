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

	// 存储当AnimationState里的动画发生更改时，许应用的mix(crossfade，交叉渐变)的持续时间
	// AnimationStateData是无状态的。同一个AnimationStateData实例可与多个AnimationStates共用。
	/// <summary>Stores mix (crossfade) durations to be applied when AnimationState animations are changed.</summary>
	public class AnimationStateData {
		internal SkeletonData skeletonData;
		readonly Dictionary<AnimationPair, float> animationToMixTime = new Dictionary<AnimationPair, float>(AnimationPairComparer.Instance);
		internal float defaultMix;

		// 当指定了动画名时，用于查找动画的SkeletonData
		/// <summary>The SkeletonData to look up animations when they are specified by name.</summary>
		public SkeletonData SkeletonData { get { return skeletonData; } }

		// 默认mix，如果没有指定mix，则使用默认mix
		/// <summary>
		/// The mix duration to use when no mix duration has been specifically defined between two animations.</summary>
		public float DefaultMix { get { return defaultMix; } set { defaultMix = value; } }

		public AnimationStateData (SkeletonData skeletonData) {
			if (skeletonData == null) throw new ArgumentException("skeletonData cannot be null.", "skeletonData");
			this.skeletonData = skeletonData;
		}

		// 设置从fromName动画到toName动画的mix
		/// <summary>Sets a mix duration by animation names.</summary>
		public void SetMix (string fromName, string toName, float duration) {
			Animation from = skeletonData.FindAnimation(fromName);
			if (from == null) throw new ArgumentException("Animation not found: " + fromName, "fromName");
			Animation to = skeletonData.FindAnimation(toName);
			if (to == null) throw new ArgumentException("Animation not found: " + toName, "toName");
			SetMix(from, to, duration);
		}

		// 设置从from动画到to动画的mix时间
		/// <summary>Sets a mix duration when changing from the specified animation to the other.
		/// See TrackEntry.MixDuration.</summary>
		public void SetMix (Animation from, Animation to, float duration) {
			if (from == null) throw new ArgumentNullException("from", "from cannot be null.");
			if (to == null) throw new ArgumentNullException("to", "to cannot be null.");
			AnimationPair key = new AnimationPair(from, to);
			animationToMixTime.Remove(key);
			animationToMixTime.Add(key, duration);
		}

		// 获取从from动画到to动画的mix时间，如果未设置，返回默认值
		/// <summary>
		/// The mix duration to use when changing from the specified animation to the other,
		/// or the DefaultMix if no mix duration has been set.
		/// </summary>
		public float GetMix (Animation from, Animation to) {
			if (from == null) throw new ArgumentNullException("from", "from cannot be null.");
			if (to == null) throw new ArgumentNullException("to", "to cannot be null.");
			AnimationPair key = new AnimationPair(from, to);
			float duration;
			if (animationToMixTime.TryGetValue(key, out duration)) return duration;
			return defaultMix;
		}

		public struct AnimationPair {
			public readonly Animation a1;
			public readonly Animation a2;

			public AnimationPair (Animation a1, Animation a2) {
				this.a1 = a1;
				this.a2 = a2;
			}

			public override string ToString () {
				return a1.name + "->" + a2.name;
			}
		}

		// Avoids boxing in the dictionary.
		public class AnimationPairComparer : IEqualityComparer<AnimationPair> {
			public static readonly AnimationPairComparer Instance = new AnimationPairComparer();

			bool IEqualityComparer<AnimationPair>.Equals (AnimationPair x, AnimationPair y) {
				return ReferenceEquals(x.a1, y.a1) && ReferenceEquals(x.a2, y.a2);
			}

			int IEqualityComparer<AnimationPair>.GetHashCode (AnimationPair obj) {
				// from Tuple.CombineHashCodes // return (((h1 << 5) + h1) ^ h2);
				int h1 = obj.a1.GetHashCode();
				return (((h1 << 5) + h1) ^ obj.a2.GetHashCode());
			}
		}
	}
}
