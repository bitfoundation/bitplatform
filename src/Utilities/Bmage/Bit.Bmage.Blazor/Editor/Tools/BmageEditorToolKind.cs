namespace Bit.Bmage;

/// <summary>Which tool is currently active in the editor.</summary>
public enum BmageEditorToolKind
{
    /// <summary>Select, move and resize existing annotations.</summary>
    Select = 0,
    /// <summary>Pan the view.</summary>
    Pan,
    /// <summary>Define a crop rectangle.</summary>
    Crop,
    /// <summary>Freehand pen.</summary>
    Draw,
    /// <summary>Straight line.</summary>
    Line,
    /// <summary>Arrow with a head at the end.</summary>
    Arrow,
    /// <summary>Rectangle outline or fill.</summary>
    Rectangle,
    /// <summary>Ellipse outline or fill.</summary>
    Ellipse,
    /// <summary>Translucent highlight block.</summary>
    Highlight,
    /// <summary>Hide content with a solid block, pixelation or blur.</summary>
    Redact,
    /// <summary>Text label.</summary>
    Text,
    /// <summary>Numbered step marker.</summary>
    Step,
    /// <summary>Paint onto a paint layer with a round brush, soft or hard, following pen pressure.</summary>
    Brush,
    /// <summary>Erase paint from the paint layer, leaving the image below untouched.</summary>
    Eraser,
    /// <summary>Pick the colour under the pointer for the drawing tools.</summary>
    Eyedropper,
    /// <summary>Fill the region of similar colour under the pointer with the fill colour, as a new layer.</summary>
    Fill,
    /// <summary>Make the region of similar colour under the pointer transparent.</summary>
    MagicEraser,
    /// <summary>Select a rectangle of pixels: Shift adds to the selection, Alt takes away from it.</summary>
    SelectRectangle,
    /// <summary>Select an ellipse of pixels.</summary>
    SelectEllipse,
    /// <summary>Select a freehand region by drawing around it.</summary>
    Lasso,
    /// <summary>Select the region of similar colour under the pointer.</summary>
    MagicWand,
}
