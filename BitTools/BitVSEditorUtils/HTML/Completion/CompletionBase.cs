using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BitVSEditorUtils.HTML.Schema;
using Microsoft.Html.Editor.Completion;
using Microsoft.Html.Editor.Completion.Def;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Web.Editor.Imaging;

namespace BitVSEditorUtils.HTML.Completion
{
    public abstract class CompletionBase : IHtmlCompletionListProvider
    {
        private readonly ImageSource _icon = GetIcon();

        private readonly ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPublic);

        public abstract string CompletionType { get; }

        public abstract IList<HtmlCompletion> GetEntries(HtmlCompletionContext context);

        public static ImageSource GetIcon()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(assembly);
            if (folder == null)
                throw new InvalidOperationException($"{nameof(folder)} is null");
            string path = Path.Combine(folder, "Bit-Foundation.png");

            Uri uri = new Uri(path);

            return BitmapFrame.Create(uri);
        }

        protected IList<HtmlCompletion> AddEntries(HtmlCompletionContext context, IEnumerable<IHtmlItem> items)
        {
            List<HtmlCompletion> list = new List<HtmlCompletion>();

            if (items == null)
                return list;

            foreach (IHtmlItem item in items)
            {
                string description = item.Description;

                if (!string.IsNullOrEmpty(item.Type))
                    description += Environment.NewLine + Environment.NewLine + "Type: " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(item.Type);

                HtmlCompletion entry = new HtmlCompletion(item.Name, item.Name, description.Trim(), _icon, null, context.Session);
                list.Add(entry);
            }

            return list;
        }

        protected IList<HtmlCompletion> AddAttributeValues(HtmlCompletionContext context, IEnumerable<string> items)
        {
            List<HtmlCompletion> list = new List<HtmlCompletion>();

            if (items == null)
                return list;

            foreach (string item in items)
            {
                HtmlCompletion entry = new HtmlCompletion(item, item, "", _glyph, null, context.Session);
                list.Add(entry);
            }

            return list;
        }
    }

}
