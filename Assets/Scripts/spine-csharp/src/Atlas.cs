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

#if (UNITY_5 || UNITY_5_3_OR_NEWER || UNITY_WSA || UNITY_WP8 || UNITY_WP8_1)
#define IS_UNITY
#endif

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

#if WINDOWS_STOREAPP
using System.Threading.Tasks;
using Windows.Storage;
#endif

namespace Spine {
	public class Atlas : IEnumerable<AtlasRegion> {
		readonly List<AtlasPage> pages = new List<AtlasPage>();
		List<AtlasRegion> regions = new List<AtlasRegion>();
		TextureLoader textureLoader;

		#region IEnumerable implementation
		public IEnumerator<AtlasRegion> GetEnumerator () {
			return regions.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () {
			return regions.GetEnumerator();
		}
		#endregion

		public List<AtlasRegion> Regions { get { return regions; } }
		public List<AtlasPage> Pages { get { return pages; } }

#if !(IS_UNITY)
#if WINDOWS_STOREAPP
		private async Task ReadFile(string path, TextureLoader textureLoader) {
			var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
			var file = await folder.GetFileAsync(path).AsTask().ConfigureAwait(false);
			using (var reader = new StreamReader(await file.OpenStreamForReadAsync().ConfigureAwait(false))) {
				try {
					Atlas atlas = new Atlas(reader, Path.GetDirectoryName(path), textureLoader);
					this.pages = atlas.pages;
					this.regions = atlas.regions;
					this.textureLoader = atlas.textureLoader;
				} catch (Exception ex) {
					throw new Exception("Error reading atlas file: " + path, ex);
				}
			}
		}

		public Atlas(string path, TextureLoader textureLoader) {
			this.ReadFile(path, textureLoader).Wait();
		}
#else
		public Atlas (string path, TextureLoader textureLoader) {
#if WINDOWS_PHONE
			Stream stream = Microsoft.Xna.Framework.TitleContainer.OpenStream(path);
			using (StreamReader reader = new StreamReader(stream)) {
#else
			using (StreamReader reader = new StreamReader(path)) {
#endif // WINDOWS_PHONE
				try {
					Atlas atlas = new Atlas(reader, Path.GetDirectoryName(path), textureLoader);
					this.pages = atlas.pages;
					this.regions = atlas.regions;
					this.textureLoader = atlas.textureLoader;
				} catch (Exception ex) {
					throw new Exception("Error reading atlas file: " + path, ex);
				}
			}
		}
#endif // WINDOWS_STOREAPP
#endif

		public Atlas (List<AtlasPage> pages, List<AtlasRegion> regions) {
			if (pages == null) throw new ArgumentNullException("pages", "pages cannot be null.");
			if (regions == null) throw new ArgumentNullException("regions", "regions cannot be null.");
			this.pages = pages;
			this.regions = regions;
			this.textureLoader = null;
		}

		/// <summary>
		/// 解析纹理图集文件并加载纹理数据，最终构造出一个Atlas对象
		/// 解析 .atlas 文件，提取页面和区域信息。
		/// 加载纹理文件，并将页面和区域信息存储到 Atlas 对象中。
		/// 支持多页面和多区域，以便在运行时高效管理纹理资源。
		/// </summary>
		/// <param name="reader">用于读取图集文件的文本内容，通常是.atlas文件</param>
		/// <param name="imagesDir">图集中纹理文件所在的目录路径，用于定位纹理文件</param>
		/// <param name="textureLoader">纹理加载器，用于加载纹理到内存中</param>
		/// <exception cref="ArgumentNullException"></exception>
		public Atlas (TextReader reader, string imagesDir, TextureLoader textureLoader) {
			if (reader == null) throw new ArgumentNullException(nameof(reader), "reader cannot be null.");
			if (imagesDir == null) throw new ArgumentNullException(nameof(imagesDir), "imagesDir cannot be null.");
			if (textureLoader == null) throw new ArgumentNullException(nameof(textureLoader), "textureLoader cannot be null.");
			
			this.textureLoader = textureLoader;
			
			// entry用于存储每一行的解析结果，键值对，entry[0] : entry[1], entry[2], entry[3], entry[4]
			// 键和值用冒号:隔开，值之间用逗号,隔开。
			// 值最多4个，多于4个会被截断为4个
			string[] entry = new string[5];
			// 当前正在解析的page对象
			AtlasPage page = null;
			// 当前正在解析的atlasRegion对象
			AtlasRegion region = null;

			// 定义page字段规则和解析逻辑
			var pageFields = new Dictionary<string, Action>(5);
			pageFields.Add("size", () => {
				page.width = int.Parse(entry[1], CultureInfo.InvariantCulture);
				page.height = int.Parse(entry[2], CultureInfo.InvariantCulture);
			});
			pageFields.Add("format", () => {
				page.format = (Format)Enum.Parse(typeof(Format), entry[1], false);
			});
			pageFields.Add("filter", () => {
				page.minFilter = (TextureFilter)Enum.Parse(typeof(TextureFilter), entry[1], false);
				page.magFilter = (TextureFilter)Enum.Parse(typeof(TextureFilter), entry[2], false);
			});
			pageFields.Add("repeat", () => {
				if (entry[1].IndexOf('x') != -1) page.uWrap = TextureWrap.Repeat;
				if (entry[1].IndexOf('y') != -1) page.vWrap = TextureWrap.Repeat;
			});
			pageFields.Add("pma", () => {
				page.pma = entry[1] == "true";
			});

			// 定义region规则和解析逻辑
			var regionFields = new Dictionary<string, Action>(8);
			regionFields.Add("xy", () => { // Deprecated, use bounds.
				region.x = int.Parse(entry[1], CultureInfo.InvariantCulture);
				region.y = int.Parse(entry[2], CultureInfo.InvariantCulture);
			});
			regionFields.Add("size", () => { // Deprecated, use bounds.
				region.width = int.Parse(entry[1], CultureInfo.InvariantCulture);
				region.height = int.Parse(entry[2], CultureInfo.InvariantCulture);
			});
			regionFields.Add("bounds", () => {
				region.x = int.Parse(entry[1], CultureInfo.InvariantCulture);
				region.y = int.Parse(entry[2], CultureInfo.InvariantCulture);
				region.width = int.Parse(entry[3], CultureInfo.InvariantCulture);
				region.height = int.Parse(entry[4], CultureInfo.InvariantCulture);
			});
			regionFields.Add("offset", () => { // Deprecated, use offsets.
				region.offsetX = int.Parse(entry[1], CultureInfo.InvariantCulture);
				region.offsetY = int.Parse(entry[2], CultureInfo.InvariantCulture);
			});
			regionFields.Add("orig", () => { // Deprecated, use offsets.
				region.originalWidth = int.Parse(entry[1], CultureInfo.InvariantCulture);
				region.originalHeight = int.Parse(entry[2], CultureInfo.InvariantCulture);
			});
			regionFields.Add("offsets", () => {
				region.offsetX = int.Parse(entry[1], CultureInfo.InvariantCulture);
				region.offsetY = int.Parse(entry[2], CultureInfo.InvariantCulture);
				region.originalWidth = int.Parse(entry[3], CultureInfo.InvariantCulture);
				region.originalHeight = int.Parse(entry[4], CultureInfo.InvariantCulture);
			});
			regionFields.Add("rotate", () => {
				string value = entry[1];
				if (value == "true")
					region.degrees = 90;
				else if (value != "false")
					region.degrees = int.Parse(value, CultureInfo.InvariantCulture);
			});
			regionFields.Add("index", () => {
				region.index = int.Parse(entry[1], CultureInfo.InvariantCulture);
			});

			// 开始解析图集元数据文件
			string line = reader.ReadLine();
			// Ignore empty lines before first entry.
			// 跳过空行
			while (line != null && line.Trim().Length == 0)
				line = reader.ReadLine();
			
			// Header entries.
			// 跳过头部字段
			while (true) {
				if (line == null || line.Trim().Length == 0) break;
				if (ReadEntry(entry, line) == 0) break; // Silently ignore all header fields.
				line = reader.ReadLine();
			}
			
			// 解析页面和区域
			// Page and region entries.
			List<string> names = null;
			List<int[]> values = null;
			while (true) {
				if (line == null) break;
				if (line.Trim().Length == 0) {
					page = null;
					line = reader.ReadLine();
				} else if (page == null) { // 当page == null时，创建一个新的AtlasPage，解析其字段，并通过TextureLoader加载纹理
					page = new AtlasPage();
					page.name = line.Trim();
					while (true) {
						if (ReadEntry(entry, line = reader.ReadLine()) == 0) break;
						Action field;
						if (pageFields.TryGetValue(entry[0], out field)) field(); // Silently ignore unknown page fields.
					}
					textureLoader.Load(page, Path.Combine(imagesDir, page.name));
					pages.Add(page);
				} else { // page != null时，创建一个新的AtlasRegion，解析其字段，并计算纹理的UV坐标
					region = new AtlasRegion();
					region.page = page;
					region.name = line;
					while (true) {
						int count = ReadEntry(entry, line = reader.ReadLine());
						if (count == 0) break;
						Action field;
						if (regionFields.TryGetValue(entry[0], out field))
							field();
						else {
							// 处理自定义字段
							if (names == null) {
								names = new List<string>(8);
								values = new List<int[]>(8);
							}
							names.Add(entry[0]);
							int[] entryValues = new int[count];
							for (int i = 0; i < count; i++)
								int.TryParse(entry[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out entryValues[i]); // Silently ignore non-integer values.
							values.Add(entryValues);
						}
					}
					if (region.originalWidth == 0 && region.originalHeight == 0) {
						region.originalWidth = region.width;
						region.originalHeight = region.height;
					}
					if (names != null && names.Count > 0) {
						region.names = names.ToArray();
						region.values = values.ToArray();
						names.Clear();
						values.Clear();
					}
					// 计算region的uv坐标
					region.u = region.x / (float)page.width;
					region.v = region.y / (float)page.height;
					if (region.degrees == 90) {
						region.u2 = (region.x + region.height) / (float)page.width;
						region.v2 = (region.y + region.width) / (float)page.height;
						(region.packedWidth, region.packedHeight) = (region.packedHeight, region.packedWidth);
					} else {
						region.u2 = (region.x + region.width) / (float)page.width;
						region.v2 = (region.y + region.height) / (float)page.height;
					}
					regions.Add(region);
				}
			}
		}

		// ReadEntry 函数的作用是解析一行数据，将其分割成键和值，并存储到 entry 数组中。它主要用于解析纹理图集文件中的字段，例如：
		// size: 1024, 768
		// filter: Linear, MipMap
		// repeat: x, y
		// 通过这个函数，可以将图集文件中的文本数据转换为程序可以处理的结构化数据。
		static private int ReadEntry (string[] entry, string line) {
			if (line == null) return 0;
			line = line.Trim();
			if (line.Length == 0) return 0;
			int colon = line.IndexOf(':');
			if (colon == -1) return 0;
			entry[0] = line.Substring(0, colon).Trim();
			for (int i = 1, lastMatch = colon + 1; ; i++) {
				int comma = line.IndexOf(',', lastMatch);
				if (comma == -1) {
					entry[i] = line.Substring(lastMatch).Trim();
					return i;
				}
				entry[i] = line.Substring(lastMatch, comma - lastMatch).Trim();
				lastMatch = comma + 1;
				if (i == 4) return 4;
			}
		}

		public void FlipV () {
			for (int i = 0, n = regions.Count; i < n; i++) {
				AtlasRegion region = regions[i];
				region.v = 1 - region.v;
				region.v2 = 1 - region.v2;
			}
		}

		/// <summary>Returns the first region found with the specified name. This method uses string comparison to find the region, so the result
		/// should be cached rather than calling this method multiple times.</summary>
		/// <returns>The region, or null.</returns>
		public AtlasRegion FindRegion (string name) {
			for (int i = 0, n = regions.Count; i < n; i++)
				if (regions[i].name == name) return regions[i];
			return null;
		}

		public void Dispose () {
			if (textureLoader == null) return;
			for (int i = 0, n = pages.Count; i < n; i++)
				textureLoader.Unload(pages[i].rendererObject);
		}
	}

	public enum Format {
		Alpha,
		Intensity,
		LuminanceAlpha,
		RGB565,
		RGBA4444,
		RGB888,
		RGBA8888
	}

	public enum TextureFilter {
		Nearest,
		Linear,
		MipMap,
		MipMapNearestNearest,
		MipMapLinearNearest,
		MipMapNearestLinear,
		MipMapLinearLinear
	}

	public enum TextureWrap {
		MirroredRepeat,
		ClampToEdge,
		Repeat
	}

	/// <summary>
	/// 纹理图集（Texture Atlas）是一种优化图形渲染性能的技术，它将多个小的纹理（比如游戏中的图标、精灵帧等）合并到一个大的纹理中。
	/// 每个大的纹理称为一个 纹理页，而 AtlasPage 就是对这些纹理页的描述。
	/// </summary>
	public class AtlasPage {
		public string name;
		public int width, height;
		public Format format = Format.RGBA8888;
		public TextureFilter minFilter = TextureFilter.Nearest;
		public TextureFilter magFilter = TextureFilter.Nearest;
		public TextureWrap uWrap = TextureWrap.ClampToEdge;
		public TextureWrap vWrap = TextureWrap.ClampToEdge;
		// 是否使用预乘透明度（Premultiplied Alpha）
		public bool pma;
		// 用于存储与具体渲染引擎相关的纹理对象
		// 加载图集时由TextureLoader设置
		public object rendererObject;

		public AtlasPage Clone () {
			return MemberwiseClone() as AtlasPage;
		}
	}

	// AtlasPage表示纹理图集中的一个纹理页，即一个大的纹理文件
	// AtlasRegion表示纹理页中的一个小区域，即从纹理页中裁剪出来的一个子纹理（通常对应一个精灵或图形资源）
	public class AtlasRegion : TextureRegion {
		// 当前region对应的page
		public AtlasPage page;
		public string name;
		public int x, y;
		
		// offsetX, offsetY 表示当前纹理区域（AtlasRegion）相对于其原始纹理（originalWidth 和 originalHeight）的偏移量。
		// 偏移量通常用于处理裁剪后的纹理，使其在渲染时能够正确对齐到原始纹理的中心或位置。
		// 如果纹理在打包时进行了裁剪（去掉了透明边缘），offsetX 和 offsetY 会记录裁剪后纹理相对于原始纹理的偏移量。
		// 渲染时需要加上这个偏移量，确保纹理在场景中的位置与原始纹理一致。
		// 举例说明：
		// 假设有一个原始纹理（originalWidth = 100，originalHeight = 100），它在打包到图集时进行了裁剪，去掉了透明边缘，裁剪后的纹理区域如下：
		// 裁剪后的纹理宽度：packedWidth = 80
		// 裁剪后的纹理高度：packedHeight = 60
		// 裁剪后纹理相对于原始纹理的偏移量：
		// offsetX = 10（裁剪掉了左侧 10 像素的透明边缘）
		// offsetY = 20（裁剪掉了顶部 20 像素的透明边缘）
		// 在这种情况下：
		// 渲染时，纹理的实际大小是 packedWidth x packedHeight（80x60）。
		// 但为了让纹理在场景中正确对齐，需要加上 offsetX 和 offsetY 的偏移量，使其看起来像是未裁剪的原始纹理。
		public float offsetX, offsetY;
		
		// originalWidth, originHeight 表示当前纹理区域在裁剪之前的原始宽度和高度。
		// 即，纹理在打包到图集之前的完整尺寸。
		public int originalWidth, originalHeight;
		
		// packedWidth, packedHeight 表示当前纹理区域在图集中实际存储的宽度和高度。
		// 即，纹理在打包到图集后占用的尺寸。
		public int packedWidth { get { return width; } set { width = value; } }
		public int packedHeight { get { return height; } set { height = value; } }
		
		// degrees 字段表示 纹理在图集中被旋转的角度，通常用于描述纹理在打包到图集时是否进行了旋转操作，以及旋转的具体角度。
		// 在纹理图集的优化过程中，为了更高效地利用图集空间，纹理可能会被旋转（通常是 90 度）。
		// degrees 字段记录了纹理在图集中被旋转的角度（以度为单位），以便在渲染时正确还原纹理的方向。
		public int degrees;
		
		// 如果纹理在打包时被旋转（rotate = true），则：
		// packedWidth 和 packedHeight 会互换。
		// 例如，裁剪后的纹理宽度为 80，高度为 60，但如果被旋转了 90 度，则：
		// packedWidth = 60
		// packedHeight = 80
		public bool rotate;
		
		public int index;
		public string[] names;
		public int[][] values;

		override public int OriginalWidth { get { return originalWidth; } }
		override public int OriginalHeight { get { return originalHeight; } }

		public AtlasRegion Clone () {
			return MemberwiseClone() as AtlasRegion;
		}
	}

	public interface TextureLoader {
		void Load (AtlasPage page, string path);
		void Unload (Object texture);
	}
}
