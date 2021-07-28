using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Sliders
{
    [TestClass]
    public class BitSliderTests : BunitTestContext
    {
        [TestInitialize]
        public void SetupJsInteropMode()
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Fluent, true),

            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Cupertino, true),

            DataRow(Visual.Material, false),
            DataRow(Visual.Material, true),
        ]
        public void BitSliderVerticalTest(Visual visual, bool vertical)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Vertical, vertical);
            });

            var bitSlider = com.Find(".bit-slider");
            Assert.IsTrue(bitSlider.ClassList.Contains($"bit-slider-{(vertical ? "column" : "row")}"));
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Fluent, true),

            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Cupertino, true),

            DataRow(Visual.Material, false),
            DataRow(Visual.Material, true),
        ]
        public void BitSliderRangedVerticalTest(Visual visual, bool vertical)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Vertical, vertical);
                parameters.Add(p => p.Ranged, true);
            });

            var bitSlider = com.Find(".bit-slider");
            Assert.IsTrue(bitSlider.ClassList.Contains($"bit-slider-ranged-{(vertical ? "column" : "row")}"));
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Fluent, true),

            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Cupertino, true),

            DataRow(Visual.Material, false),
            DataRow(Visual.Material, true),
        ]
        public void BitSliderEnabledTest(Visual visual, bool isEnabled)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitSlider = com.Find(".bit-slider");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitSlider.ClassList.Contains($"bit-slider-{isEnabledClass}-{visualClass}"));
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Fluent, true),

            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Cupertino, true),

            DataRow(Visual.Material, false),
            DataRow(Visual.Material, true),
        ]
        public void BitSliderRangedTest(Visual visual, bool ranged)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Ranged, ranged);
            });

            var bitSlider = com.Find(".bit-slider");

            Assert.IsTrue(!ranged || bitSlider.ClassList.Contains($"bit-slider-ranged-row"));
            Assert.AreEqual(bitSlider.GetElementsByTagName("input").Count(), ranged ? 2 : 1);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, 2),

            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, 2),

            DataRow(Visual.Material, null),
            DataRow(Visual.Material, 2),
        ]
        public void BitSliderDefaultLowerValueTest(Visual visual, int? defaultLowerValue)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.DefaultLowerValue, defaultLowerValue);
                parameters.Add(p => p.ShowValue, true);
                parameters.Add(p => p.Ranged, true);
            });

            var bitSlider = com.Find(".bit-slider");

            var labels = bitSlider.GetElementsByTagName("label");

            Assert.AreEqual(labels.First(l => l.ClassList.Contains("valueLabel")).TextContent, defaultLowerValue.GetValueOrDefault().ToString());
            //Assert.AreEqual(defaultLowerValue.GetValueOrDefault(), com.Instance.LowerValue);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, 2),

            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, 2),

            DataRow(Visual.Material, null),
            DataRow(Visual.Material, 2),
        ]
        public void BitSliderDefaultUpperValueTest(Visual visual, int? defaultUpperValue)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.DefaultUpperValue, defaultUpperValue);
                parameters.Add(p => p.ShowValue, true);
                parameters.Add(p => p.Ranged, true);
            });

            var bitSlider = com.Find(".bit-slider");

            var labels = bitSlider.GetElementsByTagName("label");

            Assert.AreEqual(labels.Last(l => l.ClassList.Contains("valueLabel")).TextContent, defaultUpperValue.GetValueOrDefault().ToString());
            //Assert.AreEqual(defaultUpperValue.GetValueOrDefault(), com.Instance.UpperValue);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, 2),

            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, 2),

            DataRow(Visual.Material, null),
            DataRow(Visual.Material, 2),
        ]
        public void BitSliderLowerValueTest(Visual visual, int? lowerValue)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.LowerValue, lowerValue);
                parameters.Add(p => p.UpperValue, 6);
                parameters.Add(p => p.ShowValue, true);
                parameters.Add(p => p.Ranged, true);
            });

            var bitSlider = com.Find(".bit-slider");

            var labels = bitSlider.GetElementsByTagName("label");

            Assert.AreEqual(labels.First(l => l.ClassList.Contains("valueLabel")).TextContent, lowerValue.GetValueOrDefault().ToString());
            //Assert.AreEqual(defaultLowerValue.GetValueOrDefault(), com.Instance.LowerValue);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, 2),

            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, 2),

            DataRow(Visual.Material, null),
            DataRow(Visual.Material, 2),
        ]
        public void BitSliderUpperValueTest(Visual visual, int? defaultUpperValue)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.DefaultUpperValue, defaultUpperValue);
                parameters.Add(p => p.ShowValue, true);
                parameters.Add(p => p.Ranged, true);
            });

            var bitSlider = com.Find(".bit-slider");

            var labels = bitSlider.GetElementsByTagName("label");

            Assert.AreEqual(labels.Last(l => l.ClassList.Contains("valueLabel")).TextContent, defaultUpperValue.GetValueOrDefault().ToString());
            //Assert.AreEqual(defaultUpperValue.GetValueOrDefault(), com.Instance.UpperValue);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null, null),
            DataRow(Visual.Fluent, 2, null),
            DataRow(Visual.Fluent, null, 6),
            DataRow(Visual.Fluent, 2, 6),

            DataRow(Visual.Cupertino, null, null),
            DataRow(Visual.Cupertino, 2, null),
            DataRow(Visual.Cupertino, null, 6),
            DataRow(Visual.Cupertino, 2, 6),

            DataRow(Visual.Material, null, null),
            DataRow(Visual.Material, 2, null),
            DataRow(Visual.Material, null, 6),
            DataRow(Visual.Material, 2, 6),
        ]
        public void BitSliderLowerAndUpperValueTest(Visual visual, int? lowerValue, int? upperValue)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.RangeValue, (lowerValue, upperValue));
                parameters.Add(p => p.ShowValue, true);
                parameters.Add(p => p.Ranged, true);
            });

            var bitSlider = com.Find(".bit-slider");

            var labels = bitSlider.GetElementsByTagName("label");

            Assert.AreEqual(labels.First(l => l.ClassList.Contains("valueLabel")).TextContent, lowerValue.GetValueOrDefault().ToString());
            Assert.AreEqual(labels.Last(l => l.ClassList.Contains("valueLabel")).TextContent, upperValue.GetValueOrDefault().ToString());
            //Assert.AreEqual(lowerValue.GetValueOrDefault(), com.Instance.LowerValue);
            //Assert.AreEqual(upperValue.GetValueOrDefault(), com.Instance.UpperValue);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, 2),

            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, 2),

            DataRow(Visual.Material, null),
            DataRow(Visual.Material, 2),
        ]
        public void BitSliderDefaultValueTest(Visual visual, int? defaultValue)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.DefaultValue, defaultValue);
                parameters.Add(p => p.ShowValue, true);
            });

            var bitSlider = com.Find(".bit-slider");

            var labels = bitSlider.GetElementsByTagName("label");

            Assert.AreEqual(labels.First(l => l.ClassList.Contains("valueLabel")).TextContent, defaultValue.GetValueOrDefault().ToString());
            //Assert.AreEqual(defaultValue.GetValueOrDefault(), com.Instance.Value);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, 2),

            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, 2),

            DataRow(Visual.Material, null),
            DataRow(Visual.Material, 2),
        ]
        public void BitSliderVerticalDefaultValueTest(Visual visual, int? defaultValue)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.DefaultValue, defaultValue);
                parameters.Add(p => p.Vertical, true);
                parameters.Add(p => p.ShowValue, true);
            });

            var bitSlider = com.Find(".bit-slider");

            var labels = bitSlider.GetElementsByTagName("label");

            Assert.AreEqual(labels.First(l => l.ClassList.Contains("valueLabel")).TextContent, defaultValue.GetValueOrDefault().ToString());
            //Assert.AreEqual(defaultValue.GetValueOrDefault(), com.Instance.Value);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, 2),

            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, 2),

            DataRow(Visual.Material, null),
            DataRow(Visual.Material, 2),
        ]
        public void BitSliderValueTest(Visual visual, int? value)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.ShowValue, true);
            });

            var bitSlider = com.Find(".bit-slider");

            var labels = bitSlider.GetElementsByTagName("label");

            Assert.AreEqual(labels.First(l => l.ClassList.Contains("valueLabel")).TextContent, value.GetValueOrDefault().ToString());
            //Assert.AreEqual(value.GetValueOrDefault(), com.Instance.Value);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, 2),

            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, 2),

            DataRow(Visual.Material, null),
            DataRow(Visual.Material, 2),
        ]
        public void BitSliderVerticalValueTest(Visual visual, int? value)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.Vertical, true);
                parameters.Add(p => p.ShowValue, true);
            });

            var bitSlider = com.Find(".bit-slider");

            var labels = bitSlider.GetElementsByTagName("label");

            Assert.AreEqual(labels.First(l => l.ClassList.Contains("valueLabel")).TextContent, value.GetValueOrDefault().ToString());
            //Assert.AreEqual(defaultValue.GetValueOrDefault(), com.Instance.Value);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false, null),
            DataRow(Visual.Fluent, true, null),
            DataRow(Visual.Fluent, false, 2),
            DataRow(Visual.Fluent, true, 2),

            DataRow(Visual.Cupertino, false, null),
            DataRow(Visual.Cupertino, true, null),
            DataRow(Visual.Cupertino, false, 2),
            DataRow(Visual.Cupertino, true, 2),

            DataRow(Visual.Material, false, null),
            DataRow(Visual.Material, true, null),
            DataRow(Visual.Material, false, 2),
            DataRow(Visual.Material, true, 2),
        ]
        public void BitSliderStepTest(Visual visual, bool ranged, int? step)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Ranged, ranged);
                if (step.HasValue)
                {
                    parameters.Add(p => p.Step, step.Value);
                }
            });

            var bitSlider = com.Find(".bit-slider");
            foreach (var item in bitSlider.GetElementsByTagName("input"))
            {
                Assert.AreEqual(item.GetAttribute("step"), (step.HasValue ? step.Value : com.Instance.Step).ToString());
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false, 4, 8),
            DataRow(Visual.Fluent, true, 4, 8),

            DataRow(Visual.Cupertino, false, 4, 8),
            DataRow(Visual.Cupertino, true, 4, 8),

            DataRow(Visual.Material, false, 4, 8),
            DataRow(Visual.Material, true, 4, 8)
        ]
        public void BitSliderMinMaxTest(Visual visual, bool ranged, int min, int max)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Ranged, ranged);
                parameters.Add(p => p.Min, min);
                parameters.Add(p => p.Max, max);
            });

            var bitSlider = com.Find(".bit-slider");
            foreach (var item in bitSlider.GetElementsByTagName("input"))
            {
                Assert.AreEqual(item.GetAttribute("min"), min.ToString());
                Assert.AreEqual(item.GetAttribute("max"), max.ToString());
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, null),
            DataRow(Visual.Fluent, "Bit Title"),

            DataRow(Visual.Cupertino, null),
            DataRow(Visual.Cupertino, "Bit Title"),

            DataRow(Visual.Material, null),
            DataRow(Visual.Material, "Bit Title")
        ]
        public void BitSliderLabelTest(Visual visual, string label)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Label, label);
            });

            var labelElements = com.FindAll(".bit-slider label");

            if (label.HasValue())
            {
                Assert.IsTrue(labelElements.Any(l => l.ClassList.Contains("title") && l.TextContent == label));
            }
            else
            {
                Assert.IsFalse(labelElements.Any(l => l.ClassList.Contains("title") && l.TextContent == label));
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false, false),
            DataRow(Visual.Fluent, true, false),
            DataRow(Visual.Fluent, false, true),
            DataRow(Visual.Fluent, true, true),

            DataRow(Visual.Cupertino, false, false),
            DataRow(Visual.Cupertino, true, false),
            DataRow(Visual.Cupertino, false, true),
            DataRow(Visual.Cupertino, true, true),

            DataRow(Visual.Material, false, false),
            DataRow(Visual.Material, true, false),
            DataRow(Visual.Material, false, true),
            DataRow(Visual.Material, true, true),
        ]
        public void BitSliderShowValueTest(Visual visual, bool ranged, bool showValue)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Ranged, ranged);
                parameters.Add(p => p.ShowValue, showValue);
            });

            var bitSlider = com.Find(".bit-slider");
            var labels = bitSlider.GetElementsByTagName("label");

            if (showValue)
            {
                Assert.IsTrue(labels.Any(l => l.ClassList.Contains("valueLabel")));
                Assert.AreEqual(labels.Count(l => l.ClassList.Contains("valueLabel")), ranged ? 2 : 1);
            }
            else
            {
                Assert.IsFalse(labels?.Any(l => l.ClassList.Contains("valueLabel")) ?? false);
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false, false),
            DataRow(Visual.Fluent, true, false),
            DataRow(Visual.Fluent, false, true),
            DataRow(Visual.Fluent, true, true),

            DataRow(Visual.Cupertino, false, false),
            DataRow(Visual.Cupertino, true, false),
            DataRow(Visual.Cupertino, false, true),
            DataRow(Visual.Cupertino, true, true),

            DataRow(Visual.Material, false, false),
            DataRow(Visual.Material, true, false),
            DataRow(Visual.Material, false, true),
            DataRow(Visual.Material, true, true),
        ]
        public void BitSliderOriginFromZeroTest(Visual visual, bool ranged, bool originFromZero)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Ranged, ranged);
                parameters.Add(p => p.OriginFromZero, originFromZero);
            });

            var bitSlider = com.Find(".bit-slider");
            var spans = bitSlider.GetElementsByTagName("span");

            if (originFromZero)
            {
                Assert.IsTrue(spans.Any(l => l.ClassList.Contains("zeroTick")));
            }
            else
            {
                Assert.IsFalse(spans?.Any(l => l.ClassList.Contains("zeroTick")) ?? false);
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false, null),
            DataRow(Visual.Fluent, true, null),
            DataRow(Visual.Fluent, false, "P01"),
            DataRow(Visual.Fluent, true, "P01"),

            DataRow(Visual.Cupertino, false, null),
            DataRow(Visual.Cupertino, true, null),
            DataRow(Visual.Cupertino, false, "P01"),
            DataRow(Visual.Cupertino, true, "P01"),

            DataRow(Visual.Material, false, null),
            DataRow(Visual.Material, true, null),
            DataRow(Visual.Material, false, "P01"),
            DataRow(Visual.Material, true, "P01"),
        ]
        public void BitSliderValueFormatTest(Visual visual, bool ranged, string valueFormat)
        {
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.ShowValue, true);
                parameters.Add(p => p.Ranged, ranged);
                parameters.Add(p => p.ValueFormat, valueFormat);
            });

            var bitSlider = com.Find(".bit-slider");
            var labels = bitSlider.GetElementsByTagName("label");

            if (string.IsNullOrEmpty(valueFormat))
            {
                Assert.AreEqual(labels.Count(l => l.ClassList.Contains("valueLabel") && !l.TextContent.Contains("%")), ranged ? 2 : 1);
            }
            else
            {
                Assert.AreEqual(labels.Count(l => l.ClassList.Contains("valueLabel") && l.TextContent.Contains("%")), ranged ? 2 : 1);
            }
        }
    }
}
