﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Dopamine.Controls
{
    public class AlbumLoveButton : Control
    {
        private Button albumLoveButton;
        private TextBlock heartFill;
        private TextBlock heart;

        public bool Love
        {
            get { return Convert.ToBoolean(GetValue(LoveProperty)); }
            set { SetValue(LoveProperty, value); }
        }

        public static readonly DependencyProperty LoveProperty =
            DependencyProperty.Register(nameof(Love), typeof(bool), typeof(AlbumLoveButton), new PropertyMetadata(false));

        public new double FontSize
        {
            get { return Convert.ToDouble(GetValue(FontSizeProperty)); }
            set { SetValue(FontSizeProperty, value); }
        }

        public static new readonly DependencyProperty FontSizeProperty =
           DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(AlbumLoveButton), new PropertyMetadata(14.0));

        public Brush SelectedForeground
        {
            get { return (Brush)GetValue(SelectedForegroundProperty); }
            set { SetValue(SelectedForegroundProperty, value); }
        }

        public static readonly DependencyProperty SelectedForegroundProperty =
            DependencyProperty.Register(nameof(SelectedForeground), typeof(Brush), typeof(AlbumLoveButton), new PropertyMetadata(null));

        static AlbumLoveButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AlbumLoveButton), new FrameworkPropertyMetadata(typeof(AlbumLoveButton)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.albumLoveButton = (Button)GetTemplateChild("PART_LoveButton");
            this.heartFill = (TextBlock)GetTemplateChild("PART_HeartFill");
            this.heart = (TextBlock)GetTemplateChild("PART_Heart");

            if (this.albumLoveButton != null)
            {
                this.albumLoveButton.Click += LoveButton_Click;
                this.albumLoveButton.PreviewMouseDoubleClick += LoveButton_PreviewMouseDoubleClick;
            }

            if (this.heartFill != null)
            {
                this.heartFill.MouseEnter += HeartFill_MouseEnter;
                this.heartFill.MouseLeave += HeartFill_MouseLeave;
            }

            if (this.heart != null)
            {
                this.heart.MouseEnter += Heart_MouseEnter;
                this.heart.MouseLeave += Heart_MouseLeave;
            }
        }

        private void Heart_MouseEnter(object sender, MouseEventArgs e)
        {
            this.heart.Opacity = 1.0;
        }

        private void Heart_MouseLeave(object sender, MouseEventArgs e)
        {
            this.heart.Opacity = 0.2;
        }

        private void HeartFill_MouseEnter(object sender, MouseEventArgs e)
        {
            this.heartFill.Text = char.ConvertFromUtf32(0xE00C);
        }

        private void HeartFill_MouseLeave(object sender, MouseEventArgs e)
        {
            this.heartFill.Text = char.ConvertFromUtf32(0xE0A5);
        }

        private void LoveButton_Click(object sender, RoutedEventArgs e)
        {
            this.Love = !this.Love;
        }

        private void LoveButton_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // This prevents other double click actions while rating, like playing the selected song.
            e.Handled = true;
        }
    }
}
