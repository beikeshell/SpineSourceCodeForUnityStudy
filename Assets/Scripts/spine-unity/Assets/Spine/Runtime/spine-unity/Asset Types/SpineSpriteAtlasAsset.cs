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

#if UNITY_2018_2_OR_NEWER
#define EXPOSES_SPRITE_ATLAS_UTILITIES
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace Spine.Unity {
	// 该类是一个Spine Atlas适配类，主要用于将Unity的SpriteAtlas(精灵图集)与Spine的Atlas数据结构进行结合，从而在Spine中使用Unity的精灵图集.
	/// <summary>Loads and stores a Spine atlas and list of materials.</summary>
	[CreateAssetMenu(fileName = "New Spine SpriteAtlas Asset", menuName = "Spine/Spine SpriteAtlas Asset")]
	public class SpineSpriteAtlasAsset : AtlasAssetBase {
		
		// 存储 Unity 的 SpriteAtlas 文件。它是这个类的核心资源，所有的精灵和纹理信息都来源于此。
		public SpriteAtlas spriteAtlasFile;
		
		// 存储与 SpriteAtlas 相关联的材质数组。每个材质对应一个纹理（通常一个图集只有一个纹理）。
		public Material[] materials;
		
		// Spine 的 Atlas 对象，表示 Spine 使用的图集数据结构。这个字段在运行时被动态生成。
		protected Atlas atlas;
		
		// 一个布尔值，指示是否在运行时更新图集的区域信息（通常用于调试或特定需求）。
		public bool updateRegionsInPlayMode;

		[System.Serializable]
		protected class SavedRegionInfo {
			public float x, y, width, height;
			public SpritePackingRotation packingRotation;
		}
		[SerializeField] protected SavedRegionInfo[] savedRegions;

		public override bool IsLoaded { get { return this.atlas != null; } }

		public override IEnumerable<Material> Materials { get { return materials; } }
		public override int MaterialCount { get { return materials == null ? 0 : materials.Length; } }
		public override Material PrimaryMaterial { get { return materials[0]; } }

#if UNITY_EDITOR
		static MethodInfo GetPackedSpritesMethod, GetPreviewTexturesMethod;
#if !EXPOSES_SPRITE_ATLAS_UTILITIES
		static MethodInfo PackAtlasesMethod;
#endif
#endif

		#region Runtime Instantiation
		/// <summary>
		/// Creates a runtime AtlasAsset</summary>
		public static SpineSpriteAtlasAsset CreateRuntimeInstance (SpriteAtlas spriteAtlasFile, Material[] materials, bool initialize) {
			SpineSpriteAtlasAsset atlasAsset = ScriptableObject.CreateInstance<SpineSpriteAtlasAsset>();
			atlasAsset.Reset();
			atlasAsset.spriteAtlasFile = spriteAtlasFile;
			atlasAsset.materials = materials;

			if (initialize)
				atlasAsset.GetAtlas();

			return atlasAsset;
		}
		#endregion

		void Reset () {
			Clear();
		}

		public override void Clear () {
			atlas = null;
		}

		// 返回Spine的Atlas对象。
		/// <returns>The atlas or null if it could not be loaded.</returns>
		public override Atlas GetAtlas (bool onlyMetaData = false) {
			if (spriteAtlasFile == null) {
				Debug.LogError("SpriteAtlas file not set for SpineSpriteAtlasAsset: " + name, this);
				Clear();
				return null;
			}

			if (!onlyMetaData && (materials == null || materials.Length == 0)) {
				Debug.LogError("Materials not set for SpineSpriteAtlasAsset: " + name, this);
				Clear();
				return null;
			}

			// atlas已加载，直接返回。
			if (atlas != null) return atlas;

			try {
				atlas = LoadAtlas(spriteAtlasFile);
				return atlas;
			} catch (Exception ex) {
				Debug.LogError("Error analyzing SpriteAtlas for SpineSpriteAtlasAsset: " + name + "\n" + ex.Message + "\n" + ex.StackTrace, this);
				return null;
			}
		}

		protected void AssignRegionsFromSavedRegions (Sprite[] sprites, Atlas usedAtlas) {

			if (savedRegions == null || savedRegions.Length != sprites.Length)
				return;

			int i = 0;
			foreach (var region in usedAtlas) {
				var savedRegion = savedRegions[i];
				var page = region.page;

				region.degrees = savedRegion.packingRotation == SpritePackingRotation.None ? 0 : 90;

				float x = savedRegion.x;
				float y = savedRegion.y;
				float width = savedRegion.width;
				float height = savedRegion.height;

				region.u = x / (float)page.width;
				region.v = y / (float)page.height;
				if (region.degrees == 90) {
					region.u2 = (x + height) / (float)page.width;
					region.v2 = (y + width) / (float)page.height;
				} else {
					region.u2 = (x + width) / (float)page.width;
					region.v2 = (y + height) / (float)page.height;
				}
				region.x = (int)x;
				region.y = (int)y;
				region.width = Math.Abs((int)width);
				region.height = Math.Abs((int)height);

				// flip upside down
				(region.v, region.v2) = (region.v2, region.v);

				region.originalWidth = (int)width;
				region.originalHeight = (int)height;

				// note: currently sprite pivot offsets are ignored.
				// var sprite = sprites[i];
				region.offsetX = 0;//sprite.pivot.x;
				region.offsetY = 0;//sprite.pivot.y;

				++i;
			}
		}

		// 将Unity的SpriteAtlas转换为Spine的Atlas的核心代码
		private Atlas LoadAtlas (UnityEngine.U2D.SpriteAtlas spriteAtlas) {

			List<AtlasPage> pages = new List<AtlasPage>();
			List<AtlasRegion> regions = new List<AtlasRegion>();

			Sprite[] sprites = new UnityEngine.Sprite[spriteAtlas.spriteCount];
			spriteAtlas.GetSprites(sprites);
			if (sprites.Length == 0)
				return new Atlas(pages, regions);

			Texture2D texture = null;
#if UNITY_EDITOR
			if (!Application.isPlaying)
				texture = AccessPackedTextureEditor(spriteAtlas);
			else
#endif
			texture = AccessPackedTexture(sprites);

			Material material = materials[0];
#if !UNITY_EDITOR
			material.mainTexture = texture;
#endif

			// 创建Spine的AtlasPage对象，表示Spine图集的页面信息（包括纹理、尺寸、过滤模式等）
			Spine.AtlasPage page = new AtlasPage();
			page.name = spriteAtlas.name;
			page.width = texture.width;
			page.height = texture.height;
			page.format = Spine.Format.RGBA8888;

			page.minFilter = TextureFilter.Linear;
			page.magFilter = TextureFilter.Linear;
			page.uWrap = TextureWrap.ClampToEdge;
			page.vWrap = TextureWrap.ClampToEdge;
			page.rendererObject = material;
			pages.Add(page);
			
			// 遍历所有精灵，为每个精灵创建Spine的AtlasRegion对象，表示Spine图集的一个区域
			sprites = AccessPackedSprites(spriteAtlas);
			int i = 0;
			for (; i < sprites.Length; ++i) {
				var sprite = sprites[i];
				AtlasRegion region = new AtlasRegion();
				region.name = sprite.name.Replace("(Clone)", "");
				region.page = page;
				region.degrees = sprite.packingRotation == SpritePackingRotation.None ? 0 : 90;

				region.u2 = 1;
				region.v2 = 1;
				region.width = page.width;
				region.height = page.height;
				region.originalWidth = page.width;
				region.originalHeight = page.height;

				region.index = i;
				regions.Add(region);
			}

			var atlas = new Atlas(pages, regions);
			AssignRegionsFromSavedRegions(sprites, atlas);

			return atlas;
		}

#if UNITY_EDITOR
		public static void UpdateByStartingEditorPlayMode () {
			EditorApplication.isPlaying = true;
		}

		public static bool AnySpriteAtlasNeedsRegionsLoaded () {
			string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SpineSpriteAtlasAsset");
			foreach (var guid in guids) {
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
				if (!string.IsNullOrEmpty(path)) {
					var atlasAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<SpineSpriteAtlasAsset>(path);
					if (atlasAsset) {
						if (atlasAsset.RegionsNeedLoading)
							return true;
					}
				}
			}
			return false;
		}

		public static void UpdateWhenEditorPlayModeStarted () {
			if (!EditorApplication.isPlaying)
				return;

			EditorApplication.update -= UpdateWhenEditorPlayModeStarted;
			string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SpineSpriteAtlasAsset");
			if (guids.Length == 0)
				return;

			Debug.Log("Updating SpineSpriteAtlasAssets");
			foreach (var guid in guids) {
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
				if (!string.IsNullOrEmpty(path)) {
					var atlasAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<SpineSpriteAtlasAsset>(path);
					if (atlasAsset) {
						atlasAsset.atlas = atlasAsset.LoadAtlas(atlasAsset.spriteAtlasFile);
						atlasAsset.LoadRegionsInEditorPlayMode();
						Debug.Log(string.Format("Updated regions of '{0}'", atlasAsset.name), atlasAsset);
					}
				}
			}

			EditorApplication.isPlaying = false;
		}

		public bool RegionsNeedLoading {
			get { return savedRegions == null || savedRegions.Length == 0 || updateRegionsInPlayMode; }
		}

		public void LoadRegionsInEditorPlayMode () {

			Sprite[] sprites = null;
			System.Type T = Type.GetType("UnityEditor.U2D.SpriteAtlasExtensions,UnityEditor");
			var method = T.GetMethod("GetPackedSprites", BindingFlags.NonPublic | BindingFlags.Static);
			if (method != null) {
				object retval = method.Invoke(null, new object[] { spriteAtlasFile });
				var spritesArray = retval as Sprite[];
				if (spritesArray != null && spritesArray.Length > 0) {
					sprites = spritesArray;
				}
			}
			if (sprites == null) {
				sprites = new UnityEngine.Sprite[spriteAtlasFile.spriteCount];
				spriteAtlasFile.GetSprites(sprites);
			}
			if (sprites.Length == 0) {
				Debug.LogWarning(string.Format("SpriteAtlas '{0}' contains no sprites. Please make sure all assigned images are set to import type 'Sprite'.", spriteAtlasFile.name), spriteAtlasFile);
				return;
			} else if (sprites[0].packingMode == SpritePackingMode.Tight) {
				Debug.LogError(string.Format("SpriteAtlas '{0}': Tight packing is not supported. Please disable 'Tight Packing' in the SpriteAtlas Inspector.", spriteAtlasFile.name), spriteAtlasFile);
				return;
			}

			if (savedRegions == null || savedRegions.Length != sprites.Length)
				savedRegions = new SavedRegionInfo[sprites.Length];

			int i = 0;
			foreach (var region in atlas) {
				var sprite = sprites[i];
				var rect = sprite.textureRect;
				float x = rect.min.x;
				float y = rect.min.y;
				float width = rect.width;
				float height = rect.height;

				var savedRegion = new SavedRegionInfo();
				savedRegion.x = x;
				savedRegion.y = y;
				savedRegion.width = width;
				savedRegion.height = height;
				savedRegion.packingRotation = sprite.packingRotation;
				savedRegions[i] = savedRegion;

				++i;
			}
			updateRegionsInPlayMode = false;
			AssignRegionsFromSavedRegions(sprites, atlas);
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
		}

		public static Texture2D AccessPackedTextureEditor (SpriteAtlas spriteAtlas) {
#if EXPOSES_SPRITE_ATLAS_UTILITIES
			UnityEditor.U2D.SpriteAtlasUtility.PackAtlases(new SpriteAtlas[] { spriteAtlas }, EditorUserBuildSettings.activeBuildTarget);
#else
			/*if (PackAtlasesMethod == null) {
				System.Type T = Type.GetType("UnityEditor.U2D.SpriteAtlasUtility,UnityEditor");
				PackAtlasesMethod = T.GetMethod("PackAtlases", BindingFlags.NonPublic | BindingFlags.Static);
			}
			if (PackAtlasesMethod != null) {
				PackAtlasesMethod.Invoke(null, new object[] { new SpriteAtlas[] { spriteAtlas }, EditorUserBuildSettings.activeBuildTarget });
			}*/
#endif
			if (GetPreviewTexturesMethod == null) {
				System.Type T = Type.GetType("UnityEditor.U2D.SpriteAtlasExtensions,UnityEditor");
				GetPreviewTexturesMethod = T.GetMethod("GetPreviewTextures", BindingFlags.NonPublic | BindingFlags.Static);
			}
			if (GetPreviewTexturesMethod != null) {
				object retval = GetPreviewTexturesMethod.Invoke(null, new object[] { spriteAtlas });
				var textures = retval as Texture2D[];
				if (textures.Length > 0)
					return textures[0];
			}
			return null;
		}
#endif
		public static Texture2D AccessPackedTexture (Sprite[] sprites) {
			return sprites[0].texture;
		}


		public static Sprite[] AccessPackedSprites (UnityEngine.U2D.SpriteAtlas spriteAtlas) {
			Sprite[] sprites = null;
#if UNITY_EDITOR
			if (!Application.isPlaying) {

				if (GetPackedSpritesMethod == null) {
					System.Type T = Type.GetType("UnityEditor.U2D.SpriteAtlasExtensions,UnityEditor");
					GetPackedSpritesMethod = T.GetMethod("GetPackedSprites", BindingFlags.NonPublic | BindingFlags.Static);
				}
				if (GetPackedSpritesMethod != null) {
					object retval = GetPackedSpritesMethod.Invoke(null, new object[] { spriteAtlas });
					var spritesArray = retval as Sprite[];
					if (spritesArray != null && spritesArray.Length > 0) {
						sprites = spritesArray;
					}
				}
			}
#endif
			if (sprites == null) {
				sprites = new UnityEngine.Sprite[spriteAtlas.spriteCount];
				spriteAtlas.GetSprites(sprites);
				if (sprites.Length == 0)
					return null;
			}
			return sprites;
		}
	}
}
