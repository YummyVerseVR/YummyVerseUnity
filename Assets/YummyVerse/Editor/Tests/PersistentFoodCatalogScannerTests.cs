using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.YummyServiceV2;
using YummyVerse.Scripts.View.UI;
using YummyVerse.Scripts.ViewModel.Tutorial.SO.Steps;

namespace YummyVerse.Editor.Tests
{
    public class PersistentFoodCatalogScannerTests
    {
        private string _temporaryRoot;

        [SetUp]
        public void SetUp()
        {
            _temporaryRoot = Path.Combine(
                Path.GetTempPath(),
                "YummyVerseFoodCatalogTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_temporaryRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_temporaryRoot)) Directory.Delete(_temporaryRoot, true);
        }

        [Test]
        public void ScannerUsesFolderNameModelGlbAndPngFirst()
        {
            var folder = Path.Combine(_temporaryRoot, "焼きりんご");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "model.glb"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(folder, "preview.webp"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(folder, "preview.jpeg"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(folder, "preview.jpg"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(folder, "preview.png"), new byte[] { 1 });

            var items = PersistentFoodCatalogScanner.Scan(_temporaryRoot);

            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(items[0].DisplayName, Is.EqualTo("焼きりんご"));
            Assert.That(items[0].ModelLocation, Is.EqualTo(Path.Combine(folder, "model.glb")));
            Assert.That(items[0].PreviewLocation, Is.EqualTo(Path.Combine(folder, "preview.png")));
            Assert.That(items[0].Source, Is.EqualTo(MenuItemSource.PersistentData));
            Assert.That(items[0].IsSelectable, Is.True);
        }

        [Test]
        public void ScannerSkipsFoldersWithoutModel()
        {
            var folder = Path.Combine(_temporaryRoot, "画像だけ");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "preview.png"), new byte[] { 1 });

            Assert.That(PersistentFoodCatalogScanner.Scan(_temporaryRoot), Is.Empty);
        }

        [Test]
        public void RandomSelectionOnlyReturnsPersistentFoodWithAModel()
        {
            var first = Path.Combine(_temporaryRoot, "カレー");
            var second = Path.Combine(_temporaryRoot, "寿司");
            var invalid = Path.Combine(_temporaryRoot, "モデルなし");
            Directory.CreateDirectory(first);
            Directory.CreateDirectory(second);
            Directory.CreateDirectory(invalid);
            File.WriteAllBytes(Path.Combine(first, "model.glb"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(second, "model.glb"), new byte[] { 1 });

            var selected = PersistentFoodCatalogScanner.TrySelectRandom(
                _temporaryRoot,
                new System.Random(1234),
                out var item);

            Assert.That(selected, Is.True);
            Assert.That(item, Is.Not.Null);
            Assert.That(item.Source, Is.EqualTo(MenuItemSource.PersistentData));
            Assert.That(item.ModelLocation, Does.EndWith("model.glb"));
            Assert.That(new[] { "カレー", "寿司" }, Does.Contain(item.DisplayName));
        }

        [Test]
        public void RandomSelectionReturnsFalseWhenNoPersistentFoodExists()
        {
            Assert.That(PersistentFoodCatalogScanner.TrySelectRandom(
                _temporaryRoot,
                new System.Random(1234),
                out var item), Is.False);
            Assert.That(item, Is.Null);
        }

        [Test]
        public void ChefReadyStepServesRandomPersistentFoodOnlyOnceAtS5Completion()
        {
            var chefReady = AssetDatabase.LoadAssetAtPath<NarrationStep>(
                "Assets/YummyVerse/Data/Tutorial/Steps/Step_S5_ChefReady.asset");
            var appetizerPrompt = AssetDatabase.LoadAssetAtPath<NarrationStep>(
                "Assets/YummyVerse/Data/Tutorial/Steps/Step_S6d_Appetizer.asset");

            Assert.That(chefReady, Is.Not.Null);
            Assert.That(appetizerPrompt, Is.Not.Null);
            Assert.That(
                new SerializedObject(chefReady).FindProperty("onCompletedCommand").intValue,
                Is.EqualTo((int)GameCommandId.ServeRandomPersistentFood));
            Assert.That(
                new SerializedObject(appetizerPrompt).FindProperty("onCompletedCommand").intValue,
                Is.EqualTo((int)GameCommandId.None));
        }

        [Test]
        public void FoodMenuHasExactlyFourColumns()
        {
            Assert.That(FoodSelectionMenuView.ColumnCount, Is.EqualTo(4));
        }

        [Test]
        public void PreviewDecoderRecognizesWebPByItsRiffHeader()
        {
            var webpHeader = new byte[]
            {
                (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0,
                (byte)'W', (byte)'E', (byte)'B', (byte)'P'
            };

            Assert.That(FoodPreviewTextureDecoder.IsWebP(webpHeader), Is.True);
            Assert.That(FoodPreviewTextureDecoder.IsWebP(new byte[] { 0x89, 0x50, 0x4e, 0x47 }), Is.False);
        }

        [Test]
        public void PreviewDecoderCreatesTextureFromWebP()
        {
            var bytes = Convert.FromBase64String(
                "UklGRjoAAABXRUJQVlA4IC4AAADQAQCdASoCAAIAAgA0JaACdLoB+AADsAD+h9f/KVeCxujz/7Sz9Sz9Sz/FQAAA");

            Texture2D texture = null;
            try
            {
                Assert.That(FoodPreviewTextureDecoder.TryDecode(bytes, out texture), Is.True);
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture.width, Is.EqualTo(2));
                Assert.That(texture.height, Is.EqualTo(2));
            }
            finally
            {
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void FoodMenuBuildsAVerticalFourColumnScrollGrid()
        {
            var root = new GameObject("FoodMenuTest");
            var view = root.AddComponent<FoodSelectionMenuView>();
            try
            {
                view.Initialize();

                var scroll = root.GetComponentInChildren<ScrollRect>(true);
                var grid = root.GetComponentInChildren<GridLayoutGroup>(true);
                Assert.That(scroll, Is.Not.Null);
                Assert.That(scroll.vertical, Is.True);
                Assert.That(scroll.horizontal, Is.False);
                Assert.That(grid, Is.Not.Null);
                Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
                Assert.That(grid.constraintCount, Is.EqualTo(4));
            }
            finally
            {
                view.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase("https://example.test", "https://example.test/v2/admin/menu")]
        [TestCase("https://example.test/v2/", "https://example.test/v2/admin/menu")]
        public void MenuUrlAcceptsServerRootOrV2Root(string configuredUrl, string expected)
        {
            Assert.That(YummyServiceV2Url.TryBuildMenuUrl(configuredUrl, out var actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("not-a-url")]
        [TestCase("file:///tmp/yummy-service")]
        public void MenuUrlRejectsNonHttpBaseUrls(string configuredUrl)
        {
            Assert.That(YummyServiceV2Url.TryBuildMenuUrl(configuredUrl, out _), Is.False);
        }

        [Test]
        public void AbsolutePathFromApiResponseUsesConfiguredAuthority()
        {
            Assert.That(YummyServiceV2Url.TryResolveLocation(
                "https://example.test/custom/v2",
                "/v2/admin/menu/sushi/glb",
                out var actual), Is.True);
            Assert.That(actual, Is.EqualTo("https://example.test/v2/admin/menu/sushi/glb"));
        }
    }
}
