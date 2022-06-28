namespace ImageCreator.Graphics;
using System.Drawing;
using System.Drawing.Drawing2D;

public record NewMemberBannerOptions(string AvatarUrl, string Username, string? SubHeader);

public record TextPosition(int X, int Y, string Text, string FontName = "Roboto", int FontSize = 30, int PaddingY = 20,
    FontStyle FontStyle = FontStyle.Regular);

public class Images
{
    private readonly string _backupImageUrl = "";

    public async Task<string> CreateImageAsync(TextPosition headerText, TextPosition? subHeaderText,
        string? imageUrl = null)
    {
        var background = await FetchImageAsync(imageUrl ?? _backupImageUrl);
        background = CropToBanner(background);

        background = DrawTextToImage(background, headerText);

        if (subHeaderText is not null)
            background = DrawTextToImage(background, subHeaderText);

        var path = $"{Guid.NewGuid()}.png";
        background.Save(path);
        background.Dispose();
        return await Task.FromResult(path);
    }

    public async Task<string> CreateImageAsync(NewMemberBannerOptions options, string? imageUrl = null)
    {
        var avatar = await FetchImageAsync(options.AvatarUrl);

        var background = await FetchImageAsync(imageUrl ?? _backupImageUrl);
        background = CropToBanner(background);
        
        // clip avatar to circle
        avatar = ClipImageToCircle(avatar);

        var bitmap = avatar as Bitmap;
        bitmap?.MakeTransparent();

        var banner = CopyRegionIntoImage(bitmap, background);
        banner = DrawTextToImage(banner, options.Username, options.SubHeader);

        var path = $"{Guid.NewGuid()}.png";
        banner.Save(path);

        banner.Dispose();
        avatar.Dispose();
        background.Dispose();
        bitmap?.Dispose();

        return await Task.FromResult(path);
    }

    private static Bitmap CropToBanner(Image image, int bannerWidth = 1100, int bannerHeight = 450)
    {
        var originalWidth = image.Width;
        var originalHeight = image.Height;
        
        // convert background into size
        var destinationSize = new Size(bannerWidth, bannerHeight);
        
        // ratios
        var heightRatio = (float)originalHeight / destinationSize.Height;
        var widthRatio = (float)originalWidth / destinationSize.Width;
        var ratio = Math.IEEERemainder(heightRatio, widthRatio);
        
        // scale
        var heightScale = Convert.ToInt32(destinationSize.Height * ratio);
        var widthScale = Convert.ToInt32(destinationSize.Width * ratio);

        var startX = (originalWidth - widthScale) / 2;
        var startY = (originalHeight - heightScale) / 2;

        var sourceRectangle = new Rectangle(startX, startY, widthScale, heightScale);
        var bitmap = new Bitmap(destinationSize.Width, destinationSize.Height);

        var destinationRectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        
        // apply image
        using var g = Graphics.FromImage(bitmap);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic; // highest quality possible
        g.DrawImage(image, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);
        return bitmap;
    }

    private Image ClipImageToCircle(Image image)
    {
        Image destination = new Bitmap(image.Width, image.Height, image.PixelFormat);
        var radius = image.Width / 2;
        
        // center
        var x = image.Width / 2;
        var y = image.Height / 2;

        using var g = Graphics.FromImage(destination);
        var r = new Rectangle(x - radius, y - radius, radius * 2, radius * 2);
        
        // using high quality settings
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (Brush brush = new SolidBrush(Color.Transparent))
        {
            g.FillRectangle(brush, 0, 0, destination.Width, destination.Height);
        }

        var path = new GraphicsPath();
        path.AddEllipse(r);
        g.SetClip(path);
        g.DrawImage(image, 0, 0);

        g.Dispose();
        return destination;
    }

    private Image CopyRegionIntoImage(Image source, Image destination)
    {
        using var graphicsDestination = Graphics.FromImage(destination);
        var x = (destination.Width / 2) - 100;
        var y = (destination.Height / 2) - 155;

        graphicsDestination.DrawImage(source, x, y, 220, 220);
        return destination;
    }

    private GraphicsPath MakeRoundedRect(RectangleF rect, float xRadius, float yRadius,
        bool roundTopLeft = true, bool roundTopRight = true, bool roundBottomRight = true, bool roundBottomLeft = true)
    {
       // Make a GraphicsPath to draw the rectangle.
        PointF point1, point2;
        using var path = new GraphicsPath();

        // Upper left corner.
        if (roundTopLeft)
        {
            var corner = new RectangleF(
                rect.X, rect.Y,
                2 * xRadius, 2 * yRadius);
            path.AddArc(corner, 180, 90);
            point1 = new PointF(rect.X + xRadius, rect.Y);
        }
        else point1 = new PointF(rect.X, rect.Y);

        // Top side.
        if (roundTopRight)
            point2 = new PointF(rect.Right - xRadius, rect.Y);
        else
            point2 = new PointF(rect.Right, rect.Y);
        path.AddLine(point1, point2);

        // Upper right corner.
        if (roundTopRight)
        {
            var corner = new RectangleF(
                rect.Right - 2 * xRadius, rect.Y,
                2 * xRadius, 2 * yRadius);
            path.AddArc(corner, 270, 90);
            point1 = new PointF(rect.Right, rect.Y + yRadius);
        }
        else point1 = new PointF(rect.Right, rect.Y);

        // Right side.
        if (roundBottomRight)
            point2 = new PointF(rect.Right, rect.Bottom - yRadius);
        else
            point2 = new PointF(rect.Right, rect.Bottom);
        path.AddLine(point1, point2);

        // Lower right corner.
        if (roundBottomRight)
        {
            var corner = new RectangleF(
                rect.Right - 2 * xRadius,
                rect.Bottom - 2 * yRadius,
                2 * xRadius, 2 * yRadius);
            path.AddArc(corner, 0, 90);
            point1 = new PointF(rect.Right - xRadius, rect.Bottom);
        }
        else point1 = new PointF(rect.Right, rect.Bottom);

        // Bottom side.
        if (roundBottomLeft)
            point2 = new PointF(rect.X + xRadius, rect.Bottom);
        else
            point2 = new PointF(rect.X, rect.Bottom);
        path.AddLine(point1, point2);

        // Lower left corner.
        if (roundBottomLeft)
        {
            var corner = new RectangleF(
                rect.X, rect.Bottom - 2 * yRadius,
                2 * xRadius, 2 * yRadius);
            path.AddArc(corner, 90, 90);
            point1 = new PointF(rect.X, rect.Bottom - yRadius);
        }
        else point1 = new PointF(rect.X, rect.Bottom);

        // Left side.
        if (roundTopLeft)
            point2 = new PointF(rect.X, rect.Y + yRadius);
        else
            point2 = new PointF(rect.X, rect.Y);
        path.AddLine(point1, point2);

        // Join with the start point.
        path.CloseFigure();

        return path;
    }

    private Image DrawTextToImage(Image image, TextPosition textInfo)
    {
        using var graphicsDestination = Graphics.FromImage(image);

        var font = new Font(textInfo.FontName, textInfo.FontSize, textInfo.FontStyle);
        var headerBackColor = Color.FromArgb((int)(255 * 0.42), 0, 0, 0);

        var x = (image.Width / 2) + textInfo.X;
        var y = (image.Height / 2) + textInfo.Y;
        
        var path = MakeRoundedRect(new RectangleF(x- (textInfo.Text.Length / 2 * textInfo.FontSize),
                y - (textInfo.FontSize / 2) - (textInfo.PaddingY / 2),
                textInfo.Text.Length * textInfo.FontSize,
                textInfo.FontSize + textInfo.PaddingY),
            10, 10
        );

        graphicsDestination.FillPath(new SolidBrush(headerBackColor), path);

        var drawFormat = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Alignment = StringAlignment.Center
        };

        graphicsDestination.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        graphicsDestination.DrawString(textInfo.Text, font, new SolidBrush(Color.White), x,y,drawFormat);

        return new Bitmap(image);
    }

    private Image DrawTextToImage(Image image, string header, string? subheader, int rectPaddingY = 20)
    {
        using var graphicsDestination = Graphics.FromImage(image);

        var roboto = new Font("Roboto", 30, FontStyle.Regular);
        var robotoSmall = new Font("Roboto", 23, FontStyle.Regular);
        var sample = new Bitmap(image);

        var headerBackColor = Color.FromArgb((int)(255 * 0.42), 0, 0, 0);
        
        // coordinates for header
        var headerX = image.Width / 2;
        var headerY = image.Height / 2;

        var subheaderX = image.Width / 2;
        var subheaderY = (image.Height / 2) + 160 + (robotoSmall.SizeInPoints / 2);
        
        // we need to draw rectangles so the text will appear
        var path = MakeRoundedRect(new RectangleF(headerX - (header.Length / 2 * roboto.SizeInPoints),
                headerY - (roboto.SizeInPoints / 2) - (rectPaddingY / 2),
                header.Length * roboto.SizeInPoints,
                roboto.SizeInPoints + rectPaddingY),
            10, 10);

        graphicsDestination.FillPath(new SolidBrush(headerBackColor), path);
        
        // Subheader should be optional
        if (!string.IsNullOrEmpty(subheader))
        {
            path = path = MakeRoundedRect(new RectangleF(subheaderX - (subheader.Length / 2 * robotoSmall.SizeInPoints),
                    subheaderY - (robotoSmall.SizeInPoints / 2) - (rectPaddingY / 2),
                    subheader.Length * robotoSmall.SizeInPoints,
                    robotoSmall.SizeInPoints + rectPaddingY),
                10, 10);

            graphicsDestination.FillPath(new SolidBrush(headerBackColor), path);
        }

        // determine which text color is best suited for the given image
        var headerTextColor = Color.White;
        var subheaderTextColor = Color.White;
        
        sample.Dispose();

        var drawFormat = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Alignment = StringAlignment.Center
        };
        
        // apply text
        graphicsDestination.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        graphicsDestination.DrawString(header, roboto, new SolidBrush(headerTextColor), headerX, headerY, drawFormat);
        
        if(!string.IsNullOrEmpty(subheader))
            graphicsDestination.DrawString(subheader, robotoSmall, new SolidBrush(subheaderTextColor), subheaderX, subheaderY,drawFormat);

        return new Bitmap(image);
    }

    private async Task<Image> FetchImageAsync(string url)
    {
        var client = new HttpClient();
        var response = await client.GetAsync(url);
        
        // backup image -- should never NOT have an image
        if (!response.IsSuccessStatusCode)
            response = await client.GetAsync(_backupImageUrl);

        var stream = await response.Content.ReadAsStreamAsync();
        return Image.FromStream(stream);
    }
}