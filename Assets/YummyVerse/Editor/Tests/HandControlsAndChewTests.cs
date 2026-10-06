using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Presentation;
namespace YummyVerse.Editor.Tests
{
    public class HandControlsAndChewTests
    {
        [Test] public void OpenPausesAndCloseResumesCursor()
        {
            var timeline = new ChewPlaybackTimeline(); timeline.Reset(2);
            Assert.That(timeline.Close(10), Is.True);
            timeline.Open(10.2);
            Assert.That(timeline.Cursor, Is.EqualTo(.2).Within(.00001));
            timeline.Close(20); timeline.Open(20.3);
            Assert.That(timeline.Cursor, Is.EqualTo(.5).Within(.00001));
        }
        [Test] public void DuplicateClosedDoesNotExtendBudgetAndLateUpdateCapsAtPointEight()
        {
            var timeline = new ChewPlaybackTimeline(); timeline.Reset(2); timeline.Close(10);
            Assert.That(timeline.Close(10.7), Is.False);
            Assert.That(timeline.Deadline, Is.EqualTo(10.8).Within(.00001));
            timeline.Open(15);
            Assert.That(timeline.Cursor, Is.EqualTo(.8).Within(.00001));
        }
        [Test] public void CursorWrapsAcrossClipEndAndChangingFoodResets()
        {
            var timeline = new ChewPlaybackTimeline(); timeline.Reset(.3); timeline.Close(1); timeline.Open(1.8);
            Assert.That(timeline.Cursor, Is.EqualTo(.2).Within(.00001));
            timeline.Open(2); Assert.That(timeline.Cursor, Is.EqualTo(.2).Within(.00001));
            timeline.Reset(4); Assert.That(timeline.Cursor, Is.Zero); Assert.That(timeline.IsPlaying, Is.False);
        }
        [Test] public void MenuButtonsReachEveryItemAndDisableAtBounds()
        {
            var owner = new GameObject("test-menu");
            var builder = new FoodSelectionMenuUiBuilder();
            try
            {
                var ui = builder.Build(owner.transform, _ => { });
                var items = Enumerable.Range(0,17).Select(i => new FoodCatalogItem(i.ToString(), "Food", "", "model.glb", "", MenuItemSource.PersistentData)).ToArray();
                builder.CreateCards(items, _ => { });
                Assert.That(ui.Content.Cast<Transform>().Count(c => c.gameObject.activeSelf), Is.EqualTo(8));
                Assert.That(ui.PreviousPage.interactable, Is.False);
                ui.NextPage.onClick.Invoke(); Assert.That(ui.PageText.text, Is.EqualTo("2 / 3"));
                ui.NextPage.onClick.Invoke();
                Assert.That(ui.Content.Cast<Transform>().Count(c => c.gameObject.activeSelf), Is.EqualTo(1));
                Assert.That(ui.NextPage.interactable, Is.False);
                ui.PreviousPage.onClick.Invoke(); Assert.That(ui.PageText.text, Is.EqualTo("2 / 3"));
                builder.CreateCards(Array.Empty<FoodCatalogItem>(), _ => { });
                Assert.That(ui.PageText.text, Is.EqualTo("0 / 0"));
                Assert.That(ui.NextPage.interactable, Is.False);
            }
            finally { builder.Dispose(); UnityEngine.Object.DestroyImmediate(owner); }
        }
    }
}

