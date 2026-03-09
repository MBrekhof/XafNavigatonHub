using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using DevExpress.ExpressApp.Utils;
using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.Utils.Drawing;
using DevExpress.Utils.Svg;
using XafNavigatonHub.Module.Controllers;

namespace XafNavigatonHub.Win.Editors;

public class NavigationHubControl : DevExpress.XtraEditors.XtraUserControl
{
    private NavigationHubController _controller;
    private List<HubCategoryData> _categories = new();
    private List<HubButtonData> _pinnedButtons = new();
    private readonly Panel _scrollPanel;
    private readonly ContextMenuStrip _pinMenu;
    private readonly ContextMenuStrip _unpinMenu;
    private HubButtonData _contextButton;

    // Card layout constants
    private const int CardWidth = 170;
    private const int CardHeight = 110;
    private const int CardGap = 14;
    private const int SectionGap = 28;
    private const int LeftBorderWidth = 4;
    private const int IconSize = 28;
    private const int HubPadding = 28;
    private const int CardCornerRadius = 10;
    private const int CardPaddingX = 16;
    private const int CardPaddingY = 14;

    public NavigationHubControl()
    {
        DoubleBuffered = true;
        _scrollPanel = new DoubleBufferedPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
        };
        _scrollPanel.Paint += ScrollPanel_Paint;
        _scrollPanel.MouseClick += ScrollPanel_MouseClick;
        _scrollPanel.MouseMove += ScrollPanel_MouseMove;
        _scrollPanel.MouseLeave += ScrollPanel_MouseLeave;
        _scrollPanel.Resize += (_, _) => _scrollPanel.Invalidate();
        Controls.Add(_scrollPanel);

        _pinMenu = new ContextMenuStrip();
        _pinMenu.Items.Add("Pin to Favorites", null, (_, _) => PinContextButton());

        _unpinMenu = new ContextMenuStrip();
        _unpinMenu.Items.Add("Unpin", null, (_, _) => UnpinContextButton());

        LookAndFeel.StyleChanged += (_, _) => _scrollPanel.Invalidate();
    }

    public void Initialize(NavigationHubController controller)
    {
        _controller = controller;
        RefreshData();
    }

    public void RefreshData()
    {
        if (_controller == null) return;
        _categories = _controller.GetHubData();
        var pinnedIds = _controller.GetPinnedItemIds();
        var allButtons = _categories.SelectMany(c => c.Buttons).ToList();
        _pinnedButtons = pinnedIds
            .Select(id => allButtons.FirstOrDefault(b => b.NavigationItemId == id))
            .OfType<HubButtonData>()
            .ToList();
        _cardRects.Clear();
        _hoveredCard = null;
        _scrollPanel.Invalidate();
    }

    #region Painting

    private readonly Dictionary<(HubButtonData Button, bool IsPinned), Rectangle> _cardRects = new();
    private (HubButtonData Button, bool IsPinned)? _hoveredCard;
    private readonly Dictionary<string, Image> _imageCache = new();

    private void ScrollPanel_Paint(object sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        _cardRects.Clear();

        var skin = CommonSkins.GetSkin(UserLookAndFeel.Default);
        var bgColor = GetSkinBackColor(skin);
        var cardBgColor = GetSkinCardColor(skin);
        var textColor = GetSkinTextColor(skin);
        var headerColor = GetSkinHeaderColor(skin);
        var borderColor = GetSkinBorderColor(skin);

        _scrollPanel.BackColor = bgColor;

        int y = HubPadding;
        int availableWidth = _scrollPanel.ClientSize.Width - HubPadding * 2;

        // Pinned section
        if (_pinnedButtons.Count > 0)
        {
            y = PaintSectionHeader(g, "\u2605  Preferred Actions", y, headerColor);
            y = PaintCardGrid(g, _pinnedButtons, true, y, availableWidth, cardBgColor, textColor, borderColor);
            y += SectionGap;
        }

        // Category sections
        foreach (var category in _categories)
        {
            y = PaintSectionHeader(g, category.Caption, y, headerColor);
            y = PaintCardGrid(g, category.Buttons, false, y, availableWidth, cardBgColor, textColor, borderColor);
            y += SectionGap;
        }

        // Set auto-scroll size
        _scrollPanel.AutoScrollMinSize = new Size(0, y + HubPadding);
    }

    private int PaintSectionHeader(Graphics g, string text, int y, Color headerColor)
    {
        int adjustedY = y + _scrollPanel.AutoScrollPosition.Y;
        using var font = new Font("Segoe UI", 13f, FontStyle.Bold);
        g.DrawString(text, font, new SolidBrush(headerColor), HubPadding, adjustedY);
        return y + 36;
    }

    private int PaintCardGrid(Graphics g, List<HubButtonData> buttons, bool isPinned, int startY,
        int availableWidth, Color cardBg, Color textColor, Color borderColor)
    {
        int cardsPerRow = Math.Max(1, (availableWidth + CardGap) / (CardWidth + CardGap));
        int y = startY;
        int col = 0;

        foreach (var button in buttons)
        {
            int x = HubPadding + col * (CardWidth + CardGap);
            int adjustedY = y + _scrollPanel.AutoScrollPosition.Y;

            var rect = new Rectangle(x, adjustedY, CardWidth, CardHeight);
            var key = (button, isPinned);
            _cardRects[key] = rect;

            bool isHovered = _hoveredCard.HasValue &&
                             _hoveredCard.Value.Button == button &&
                             _hoveredCard.Value.IsPinned == isPinned;

            PaintCard(g, button, rect, isHovered, cardBg, textColor, borderColor);

            col++;
            if (col >= cardsPerRow)
            {
                col = 0;
                y += CardHeight + CardGap;
            }
        }

        if (col > 0) y += CardHeight + CardGap;
        return y;
    }

    private void PaintCard(Graphics g, HubButtonData button, Rectangle rect,
        bool isHovered, Color cardBg, Color textColor, Color borderColor)
    {
        var accentColor = ParseColor(button.Color, Color.FromArgb(25, 118, 210));

        // Shadow
        if (isHovered)
        {
            using var shadowPath = CreateRoundedRect(new Rectangle(rect.X + 2, rect.Y + 3, rect.Width, rect.Height), CardCornerRadius);
            using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
            g.FillPath(shadowBrush, shadowPath);
        }
        else
        {
            using var shadowPath = CreateRoundedRect(new Rectangle(rect.X + 1, rect.Y + 1, rect.Width, rect.Height), CardCornerRadius);
            using var shadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0));
            g.FillPath(shadowBrush, shadowPath);
        }

        // Card background
        using var cardPath = CreateRoundedRect(rect, CardCornerRadius);
        using var cardBrush = new SolidBrush(isHovered ? LightenColor(cardBg, 8) : cardBg);
        g.FillPath(cardBrush, cardPath);

        // Border
        using var borderPen = new Pen(isHovered ? Color.FromArgb(80, accentColor) : borderColor, 1f);
        g.DrawPath(borderPen, cardPath);

        // Left accent border
        var accentRect = new Rectangle(rect.X, rect.Y, LeftBorderWidth, rect.Height);
        using var accentPath = CreateLeftRoundedRect(accentRect, CardCornerRadius);
        using var accentBrush = new SolidBrush(accentColor);
        g.FillPath(accentBrush, accentPath);

        // Icon
        int contentX = rect.X + LeftBorderWidth + CardPaddingX;
        int iconY = rect.Y + CardPaddingY + 4;
        var image = GetButtonImage(button);
        if (image != null)
        {
            int iconX = contentX + (rect.Width - LeftBorderWidth - CardPaddingX * 2 - IconSize) / 2;
            g.DrawImage(image, new Rectangle(iconX, iconY, IconSize, IconSize));
        }

        // Caption
        int captionY = iconY + IconSize + 10;
        int captionWidth = rect.Width - LeftBorderWidth - CardPaddingX * 2;
        using var captionFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        using var captionBrush = new SolidBrush(textColor);
        var captionRect = new RectangleF(contentX, captionY, captionWidth, rect.Bottom - captionY - CardPaddingY);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.EllipsisWord
        };
        g.DrawString(button.Caption, captionFont, captionBrush, captionRect, sf);

        // External link indicator
        if (!string.IsNullOrEmpty(button.ExternalUrl))
        {
            using var extFont = new Font("Segoe UI", 11f, FontStyle.Regular);
            using var extBrush = new SolidBrush(isHovered ? accentColor : Color.FromArgb(160, textColor));
            g.DrawString("\u2197", extFont, extBrush, rect.Right - 18, rect.Y + 4);
        }
    }

    private Image GetButtonImage(HubButtonData button)
    {
        if (string.IsNullOrEmpty(button.ImageName)) return null;
        if (_imageCache.TryGetValue(button.ImageName, out var cached)) return cached;

        Image img = null;
        try
        {
            // Try ImageLoader for DevExpress-registered images
            var imageInfo = ImageLoader.Instance.GetLargeImageInfo(button.ImageName);
            if (imageInfo.IsEmpty)
                imageInfo = ImageLoader.Instance.GetImageInfo(button.ImageName);

            if (!imageInfo.IsEmpty)
            {
                if (imageInfo.IsSvgImage && imageInfo.ImageBytes is { Length: > 0 } svgBytes)
                {
                    using var ms = new MemoryStream(svgBytes);
                    var svgImage = SvgImage.FromStream(ms);
                    var palette = SvgPaletteHelper.GetSvgPalette(UserLookAndFeel.Default, ObjectState.Normal);
                    img = svgImage.Render(new Size(IconSize * 2, IconSize * 2), palette);
                }
                else if (imageInfo.ImageBytes is { Length: > 0 } pngBytes)
                {
                    using var ms = new MemoryStream(pngBytes);
                    img = new Bitmap(Image.FromStream(ms), new Size(IconSize, IconSize));
                }
                else if (!imageInfo.IsUrlEmpty && !string.IsNullOrEmpty(button.ImageUrl))
                {
                    img = DecodeDataUri(button.ImageUrl);
                }
            }

            if (img == null && !string.IsNullOrEmpty(button.ImageUrl))
            {
                img = DecodeDataUri(button.ImageUrl);
            }
        }
        catch
        {
            // Silently ignore image loading errors
        }

        if (img != null)
            _imageCache[button.ImageName] = img;
        return img;
    }

    private Image DecodeDataUri(string dataUri)
    {
        if (string.IsNullOrEmpty(dataUri) || !dataUri.StartsWith("data:")) return null;
        try
        {
            var commaIndex = dataUri.IndexOf(',');
            if (commaIndex < 0) return null;
            var base64 = dataUri[(commaIndex + 1)..];
            var bytes = Convert.FromBase64String(base64);

            if (dataUri.Contains("svg+xml"))
            {
                using var ms = new MemoryStream(bytes);
                var svgImage = SvgImage.FromStream(ms);
                var palette = SvgPaletteHelper.GetSvgPalette(UserLookAndFeel.Default, ObjectState.Normal);
                return svgImage.Render(new Size(IconSize * 2, IconSize * 2), palette);
            }
            else
            {
                using var ms = new MemoryStream(bytes);
                return new Bitmap(Image.FromStream(ms), new Size(IconSize, IconSize));
            }
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Mouse Interaction

    private void ScrollPanel_MouseMove(object sender, MouseEventArgs e)
    {
        (HubButtonData Button, bool IsPinned)? newHover = null;
        foreach (var kvp in _cardRects)
        {
            if (kvp.Value.Contains(e.Location))
            {
                newHover = kvp.Key;
                break;
            }
        }

        if (newHover != _hoveredCard)
        {
            _hoveredCard = newHover;
            _scrollPanel.Cursor = _hoveredCard.HasValue ? Cursors.Hand : Cursors.Default;
            _scrollPanel.Invalidate();
        }
    }

    private void ScrollPanel_MouseLeave(object sender, EventArgs e)
    {
        if (_hoveredCard != null)
        {
            _hoveredCard = null;
            _scrollPanel.Cursor = Cursors.Default;
            _scrollPanel.Invalidate();
        }
    }

    private void ScrollPanel_MouseClick(object sender, MouseEventArgs e)
    {
        foreach (var kvp in _cardRects)
        {
            if (kvp.Value.Contains(e.Location))
            {
                var button = kvp.Key.Button;
                bool isPinned = kvp.Key.IsPinned;

                if (e.Button == MouseButtons.Left)
                {
                    if (!string.IsNullOrEmpty(button.ExternalUrl))
                    {
                        Process.Start(new ProcessStartInfo(button.ExternalUrl) { UseShellExecute = true });
                    }
                    else
                    {
                        _controller?.NavigateToItem(button.NavigationItemId);
                    }
                }
                else if (e.Button == MouseButtons.Right)
                {
                    _contextButton = button;
                    if (isPinned)
                        _unpinMenu.Show(_scrollPanel, e.Location);
                    else
                        _pinMenu.Show(_scrollPanel, e.Location);
                }
                return;
            }
        }
    }

    private void PinContextButton()
    {
        if (_contextButton == null || _controller == null) return;
        var pinned = _controller.GetPinnedItemIds();
        if (!pinned.Contains(_contextButton.NavigationItemId))
        {
            pinned.Add(_contextButton.NavigationItemId);
            _controller.SetPinnedItems(pinned);
            RefreshData();
        }
        _contextButton = null;
    }

    private void UnpinContextButton()
    {
        if (_contextButton == null || _controller == null) return;
        var pinned = _controller.GetPinnedItemIds();
        pinned.Remove(_contextButton.NavigationItemId);
        _controller.SetPinnedItems(pinned);
        RefreshData();
        _contextButton = null;
    }

    #endregion

    #region Skin Helpers

    private static Color GetSkinBackColor(Skin skin)
    {
        try
        {
            var element = skin[CommonSkins.SkinForm];
            if (element != null)
                return element.Color.BackColor;
        }
        catch { }
        return SystemColors.Control;
    }

    private static Color GetSkinCardColor(Skin skin)
    {
        try
        {
            var element = skin[CommonSkins.SkinHighlightedItem];
            if (element != null)
            {
                var c = element.Color.BackColor;
                if (c != Color.Empty && c.A > 0) return c;
            }
        }
        catch { }
        // Derive from form color: if dark theme, use slightly lighter; if light, use white
        var bg = GetSkinBackColor(skin);
        return bg.GetBrightness() < 0.5f
            ? Color.FromArgb(255, Math.Min(bg.R + 25, 255), Math.Min(bg.G + 25, 255), Math.Min(bg.B + 25, 255))
            : Color.White;
    }

    private static Color GetSkinTextColor(Skin skin)
    {
        try
        {
            var element = skin[CommonSkins.SkinLabel];
            if (element != null)
            {
                var c = element.Color.ForeColor;
                if (c != Color.Empty && c.A > 0) return c;
            }
        }
        catch { }
        var bg = GetSkinBackColor(skin);
        return bg.GetBrightness() < 0.5f ? Color.FromArgb(230, 230, 230) : Color.FromArgb(50, 50, 50);
    }

    private static Color GetSkinHeaderColor(Skin skin)
    {
        var text = GetSkinTextColor(skin);
        return Color.FromArgb(180, text);
    }

    private static Color GetSkinBorderColor(Skin skin)
    {
        var bg = GetSkinBackColor(skin);
        return bg.GetBrightness() < 0.5f
            ? Color.FromArgb(60, 255, 255, 255)
            : Color.FromArgb(224, 224, 224);
    }

    #endregion

    #region Drawing Helpers

    private static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static GraphicsPath CreateLeftRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color ParseColor(string colorStr, Color fallback)
    {
        if (string.IsNullOrEmpty(colorStr)) return fallback;
        try
        {
            if (colorStr.StartsWith("#"))
                return ColorTranslator.FromHtml(colorStr);
            return Color.FromName(colorStr);
        }
        catch
        {
            return fallback;
        }
    }

    private static Color LightenColor(Color color, int amount)
    {
        return Color.FromArgb(color.A,
            Math.Min(color.R + amount, 255),
            Math.Min(color.G + amount, 255),
            Math.Min(color.B + amount, 255));
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var img in _imageCache.Values)
                img?.Dispose();
            _imageCache.Clear();
            _pinMenu?.Dispose();
            _unpinMenu?.Dispose();
        }
        base.Dispose(disposing);
    }

    private class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
    }
}
