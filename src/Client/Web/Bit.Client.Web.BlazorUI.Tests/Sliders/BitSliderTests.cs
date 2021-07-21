using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Sliders
{
    [TestClass]
    public class BitSliderTests : BunitTestContext
    {
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
        public void BitSliderEnabledHorizontalTest(Visual visual, bool isEnabled)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitSlider = com.Find(".bit-slider");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitSlider.ClassList.Contains($"bit-slider-{isEnabledClass}-{visualClass}"));

            //Assert.IsTrue(isEnabled ? com.Instance.Value.HasValue : !com.Instance.Value.HasValue);
            //Assert.AreEqual(isEnabled ? 0 : null, com.Instance.Value);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Fluent, true),

            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Cupertino, true),

            DataRow(Visual.Material, false),
            DataRow(Visual.Material, true),
        ]
        public void BitSliderEnabledVerticalTest(Visual visual, bool isEnabled)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.Vertical, true);
            });

            var bitSlider = com.Find(".bit-slider");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitSlider.ClassList.Contains($"bit-slider-{isEnabledClass}-{visualClass}"));

            //Assert.IsTrue(isEnabled ? com.Instance.Value.HasValue : !com.Instance.Value.HasValue);
            //Assert.AreEqual(isEnabled ? 0 : null, com.Instance.Value);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Fluent, true),

            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Cupertino, true),

            DataRow(Visual.Material, false),
            DataRow(Visual.Material, true),
        ]
        public void BitSliderEnabledHorizontalRangedTest(Visual visual, bool isEnabled)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.Ranged, true);
            });


            var bitSlider = com.Find(".bit-slider");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitSlider.ClassList.Contains($"bit-slider-{isEnabledClass}-{visualClass}"));

            //Assert.IsTrue(isEnabled ? com.Instance.LowerValue.HasValue : !com.Instance.LowerValue.HasValue);
            //Assert.IsTrue(isEnabled ? com.Instance.UpperValue.HasValue : !com.Instance.UpperValue.HasValue);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Fluent, true),

            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Cupertino, true),

            DataRow(Visual.Material, false),
            DataRow(Visual.Material, true),
        ]
        public void BitSliderEnabledVerticalRangedTest(Visual visual, bool isEnabled)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.Vertical, true);
                parameters.Add(p => p.Ranged, true);
            });

            var bitSlider = com.Find(".bit-slider");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitSlider.ClassList.Contains($"bit-slider-{isEnabledClass}-{visualClass}"));

            //Assert.IsTrue(isEnabled ? com.Instance.LowerValue.HasValue : !com.Instance.LowerValue.HasValue);
            //Assert.IsTrue(isEnabled ? com.Instance.UpperValue.HasValue : !com.Instance.UpperValue.HasValue);
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Ranged, ranged);
            });

            var bitSlider = com.Find(".bit-slider");

            Assert.IsTrue(!ranged || bitSlider.ClassList.Contains($"bit-slider-ranged-row"));
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false),
            DataRow(Visual.Fluent, true),

            DataRow(Visual.Cupertino, false),
            DataRow(Visual.Cupertino, true),

            DataRow(Visual.Material, false),
            DataRow(Visual.Material, true),
        ]
        public void BitSliderVerticalRangedTest(Visual visual, bool ranged)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Ranged, ranged);
                parameters.Add(p => p.Vertical, true);
            });

            var bitSlider = com.Find(".bit-slider");

            Assert.IsTrue(!ranged || bitSlider.ClassList.Contains($"bit-slider-ranged-column"));
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
        public void BitSliderUpperValueTest(Visual visual, int? lowerValue, int? upperValue)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
            DataRow(Visual.Fluent, false, null, null),
            DataRow(Visual.Fluent, false, 4, null),
            DataRow(Visual.Fluent, false, null, 8),
            DataRow(Visual.Fluent, true, null, null),
            DataRow(Visual.Fluent, true, 4, null),
            DataRow(Visual.Fluent, true, null, 8),
            DataRow(Visual.Fluent, false, 4, 8),
            DataRow(Visual.Fluent, true, 4, 8),

            DataRow(Visual.Cupertino, false, null, null),
            DataRow(Visual.Cupertino, false, 4, null),
            DataRow(Visual.Cupertino, false, null, 8),
            DataRow(Visual.Cupertino, true, null, null),
            DataRow(Visual.Cupertino, true, 4, null),
            DataRow(Visual.Cupertino, true, null, 8),
            DataRow(Visual.Cupertino, false, 4, 8),
            DataRow(Visual.Cupertino, true, 4, 8),

            DataRow(Visual.Material, false, null, null),
            DataRow(Visual.Material, false, 4, null),
            DataRow(Visual.Material, false, null, 8),
            DataRow(Visual.Material, true, null, null),
            DataRow(Visual.Material, true, 4, null),
            DataRow(Visual.Material, true, null, 8),
            DataRow(Visual.Material, false, 4, 8),
            DataRow(Visual.Material, true, 4, 8),
        ]
        public void BitSliderMinMaxTest(Visual visual, bool ranged, int? min, int? max)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Ranged, ranged);
                if (min.HasValue)
                {
                    parameters.Add(p => p.Min, min.Value);
                }

                if (max.HasValue)
                {
                    parameters.Add(p => p.Max, max.Value);
                }
            });

            var bitSlider = com.Find(".bit-slider");
            foreach (var item in bitSlider.GetElementsByTagName("input"))
            {
                Assert.AreEqual(item.GetAttribute("min"), (min.HasValue ? min.Value : com.Instance.Min).ToString());
                Assert.AreEqual(item.GetAttribute("max"), (max.HasValue ? max.Value : com.Instance.Max).ToString());
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, false, null),
            DataRow(Visual.Fluent, true, null),
            DataRow(Visual.Fluent, false, "Bit Title"),
            DataRow(Visual.Fluent, true, "Bit Title"),

            DataRow(Visual.Cupertino, false, null),
            DataRow(Visual.Cupertino, true, null),
            DataRow(Visual.Cupertino, false, "Bit Title"),
            DataRow(Visual.Cupertino, true, "Bit Title"),

            DataRow(Visual.Material, false, null),
            DataRow(Visual.Material, true, null),
            DataRow(Visual.Material, false, "Bit Title"),
            DataRow(Visual.Material, true, "Bit Title"),
        ]
        public void BitSliderLabelTest(Visual visual, bool ranged, string label)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Ranged, ranged);
                parameters.Add(p => p.Label, label);
            });

            var bitSlider = com.Find(".bit-slider");
            var labels = bitSlider.GetElementsByTagName("label");

            if (string.IsNullOrEmpty(label))
            {
                Assert.IsFalse(labels.Any(l => l.ClassList.Contains("title")));
            }
            else
            {
                Assert.IsTrue(labels.Any(l => l.ClassList.Contains("title") && l.TextContent.Equals(label)));
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var com = RenderComponent<BitSliderTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Ranged, ranged);
                parameters.Add(p => p.OriginFromZero, originFromZero);
            });

            var bitSlider = com.Find(".bit-slider");
            var labels = bitSlider.GetElementsByTagName("span");

            if (originFromZero)
            {
                Assert.IsTrue(labels.Any(l => l.ClassList.Contains("zeroTick")));
            }
            else
            {
                Assert.IsFalse(labels?.Any(l => l.ClassList.Contains("zeroTick")) ?? false);
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
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
