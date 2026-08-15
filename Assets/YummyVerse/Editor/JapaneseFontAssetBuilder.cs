using System.Globalization;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace YummyVerse.Editor
{
    /// <summary>
    /// 日本語 TMP フォントアセットを japanese_full.txt の文字セットで生成し直す。
    ///
    /// Font Asset Creator ウィンドウを経由しないので、8192 アトラス生成後の再描画で
    /// エディタが落ちる問題を踏まない。既存アセットがある場合は中身だけを差し替えるため、
    /// アセットの GUID とマテリアルの fileID が変わらず、参照済みのプレハブが壊れない。
    /// </summary>
    public static class JapaneseFontAssetBuilder
    {
        private const string SourceFontPath = "Assets/YummyVerse/Misc/MPLUS1p-Regular.ttf";
        private const string CharacterSetPath = "Assets/YummyVerse/Misc/japanese_full.txt";
        private const string OutputPath = "Assets/YummyVerse/Misc/MPLUS1p-Regular SDF.asset";

        // 7,139 字を 4096x4096 の 1 枚に収めるための値。
        // padding は SDF の距離場の幅になるので、point size の 1 割程度が必要。
        private const int SamplingPointSize = 36;
        private const int Padding = 4;
        private const int AtlasSize = 4096;

        [UnityEditor.MenuItem("YummyVerse/Fonts/Rebuild Japanese Font Asset")]
        public static void Rebuild()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (font == null)
            {
                Debug.LogError($"[JapaneseFontAssetBuilder] ソースフォントが見つからない: {SourceFontPath}");
                return;
            }

            var characterSet = AssetDatabase.LoadAssetAtPath<TextAsset>(CharacterSetPath);
            if (characterSet == null)
            {
                Debug.LogError($"[JapaneseFontAssetBuilder] 文字セットが見つからない: {CharacterSetPath}");
                return;
            }

            // 改行と不可視の書式制御文字を除いて重複を潰す。
            // U+3000(全角スペース)は必要なので、空白全般を落とさないよう改行だけを対象にする。
            var characters = new string(characterSet.text
                .Where(c => c != '\n' && c != '\r' && c != '\t')
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.Format)
                .Distinct()
                .ToArray());

            var source = TMP_FontAsset.CreateFontAsset(
                font, SamplingPointSize, Padding, GlyphRenderMode.SDFAA,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);

            if (source == null)
            {
                Debug.LogError("[JapaneseFontAssetBuilder] フォントフェイスを読めなかった。" +
                               "ttf のインポート設定で Include Font Data が有効か確認すること。");
                return;
            }

            source.TryAddCharacters(characters, out var missingCharacters, includeFontFeatures: true);
            source.atlasPopulationMode = AtlasPopulationMode.Static;

            var target = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);
            if (target == null)
                CreateNew(source);
            else
                Overwrite(source, target);

            var atlasCount = (target == null ? source : target).atlasTextures.Length;
            var missingCount = string.IsNullOrEmpty(missingCharacters) ? 0 : missingCharacters.Length;

            Debug.Log($"[JapaneseFontAssetBuilder] {OutputPath}\n" +
                      $"要求 {characters.Length} 字 / 登録 {characters.Length - missingCount} 字 / 未登録 {missingCount} 字\n" +
                      $"{SamplingPointSize}pt, padding {Padding}, SDFAA, {AtlasSize}x{AtlasSize} x {atlasCount} 枚");

            if (missingCount > 0)
                Debug.LogWarning($"[JapaneseFontAssetBuilder] アトラスに入らなかった、または" +
                                 $"ソースフォントが持っていない文字:\n{missingCharacters}");
        }

        private static void CreateNew(TMP_FontAsset source)
        {
            source.name = System.IO.Path.GetFileNameWithoutExtension(OutputPath);
            AssetDatabase.CreateAsset(source, OutputPath);

            AttachAtlasTextures(source);

            source.material.name = source.name + " Material";
            source.material.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(source.material, source);

            Finish(source);
        }

        /// <summary>
        /// 既存アセットの中身だけを入れ替える。マテリアルは使い回すので
        /// プレハブ側の m_sharedMaterial (fileID 参照) が生き残る。
        /// </summary>
        private static void Overwrite(TMP_FontAsset source, TMP_FontAsset target)
        {
            var assetName = target.name;
            var material = target.material;

            // atlasTextures は内部配列をそのまま返すので、CopySerialized が同じ配列インスタンスを
            // 使い回すと退避したつもりの参照まで新しい方に書き換わる。中身をコピーして逃がしておく。
            var previousTextures = target.atlasTextures == null
                ? new Texture2D[0]
                : (Texture2D[])target.atlasTextures.Clone();
            var newTextures = (Texture2D[])source.atlasTextures.Clone();

            EditorUtility.CopySerialized(source, target);
            target.name = assetName;
            target.hideFlags = HideFlags.None;

            foreach (var texture in previousTextures)
            {
                if (texture != null && System.Array.IndexOf(newTextures, texture) < 0)
                    Object.DestroyImmediate(texture, true);
            }

            target.atlasTextures = newTextures;
            AttachAtlasTextures(target);

            // CopySerialized で source 側の一時マテリアルを指しているので、既存のものに戻す。
            var copiedMaterial = target.material;
            if (copiedMaterial != null && copiedMaterial != material && !AssetDatabase.Contains(copiedMaterial))
                Object.DestroyImmediate(copiedMaterial);

            target.material = material;
            var atlas = target.atlasTextures[0];
            material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
            material.SetFloat(ShaderUtilities.ID_TextureWidth, atlas.width);
            material.SetFloat(ShaderUtilities.ID_TextureHeight, atlas.height);
            material.SetFloat(ShaderUtilities.ID_GradientScale, Padding + 1);

            Object.DestroyImmediate(source);

            Finish(target);
        }

        private static void AttachAtlasTextures(TMP_FontAsset fontAsset)
        {
            var textures = fontAsset.atlasTextures;
            for (var i = 0; i < textures.Length; i++)
            {
                textures[i].name = fontAsset.name + " Atlas" + (i == 0 ? string.Empty : " " + i);
                textures[i].hideFlags = HideFlags.None;

                if (!AssetDatabase.Contains(textures[i]))
                    AssetDatabase.AddObjectToAsset(textures[i], fontAsset);
            }
        }

        private static void Finish(TMP_FontAsset fontAsset)
        {
            fontAsset.ReadFontAssetDefinition();

            EditorUtility.SetDirty(fontAsset);
            foreach (var texture in fontAsset.atlasTextures)
                EditorUtility.SetDirty(texture);
            EditorUtility.SetDirty(fontAsset.material);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(OutputPath);
        }
    }
}
