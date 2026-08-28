using System.Linq;
using NUnit.Framework;
using UnityEngine;
using YummyVerse.Scripts.InputActions;

namespace YummyVerse.Editor.Tests
{
    public class InputLayerTests
    {
        [Test]
        public void RestaurantBindings_UseThumbButtonsAndUnorderedAAndXChord()
        {
            var input = new RestaurantInput();
            try
            {
                Assert.That(input.asset.FindActionMap("Menu"), Is.Null);
                Assert.That(input.asset.FindAction("DestroyFood"), Is.Null);

                var menuBindings = input.Eating.TurnOnMenu.bindings;
                Assert.That(menuBindings.Count, Is.EqualTo(3));
                Assert.That(menuBindings[0].isComposite, Is.True);
                Assert.That(menuBindings[0].path, Is.EqualTo("OneModifier(modifiersOrder=2)"));
                Assert.That(menuBindings[1].isPartOfComposite, Is.True);
                Assert.That(menuBindings[1].path, Is.EqualTo("<XRController>{LeftHand}/primaryButton"));
                Assert.That(menuBindings[2].isPartOfComposite, Is.True);
                Assert.That(menuBindings[2].path, Is.EqualTo("<XRController>{RightHand}/primaryButton"));

                var startBindings = input.Eating.Start.bindings;
                var aButton = startBindings.Single(binding =>
                    binding.path == "<XRController>{RightHand}/primaryButton");
                var bButton = startBindings.Single(binding =>
                    binding.path == "<XRController>{RightHand}/secondaryButton");

                Assert.That(aButton.interactions, Is.EqualTo("Press(behavior=1)"));
                Assert.That(string.IsNullOrEmpty(bButton.interactions), Is.True);
                Assert.That(startBindings.Any(binding => binding.path == "<XRController>{RightHand}/triggerPressed"),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(input.asset);
            }
        }
    }
}
