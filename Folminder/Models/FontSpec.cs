using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.RightsManagement;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Folminder.Models
{
    public sealed record FontSpec
    {
        public FontFamily FontFamily { get; }
        public double FontSize { get; }
        public FontWeight FontWeight { get; }
        public FontStyle FontStyle { get; }
        public FontStretch FontStretch { get; }

        public Typeface Typeface => _typeface;

        private readonly Typeface _typeface;

        private readonly double _pixelsPerDip;

        public FontSpec(Control control)
        {
            this.FontFamily = control.FontFamily;
            this.FontSize = control.FontSize;
            this.FontWeight = control.FontWeight;
            this.FontStyle = control.FontStyle;
            this.FontStretch = control.FontStretch;
            _typeface = new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch);
            _pixelsPerDip = VisualTreeHelper.GetDpi(control).PixelsPerDip;
        }

        public FormattedText CreateFormattedText(
            string text,
            CultureInfo? culture = null,
            Brush? brush = null,
            double pixelsPerDip = 1.0)
        {
            culture ??= CultureInfo.CurrentCulture;
            brush ??= Brushes.Black;

            return new FormattedText(
                text,
                culture,
                FlowDirection.LeftToRight,
                Typeface,
                FontSize,
                brush,
                pixelsPerDip);
        }
    }
}
