﻿using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace GitHub.VisualStudio.Views.GitHubPane
{
    public class QuickListPresenter : Panel, IScrollInfo
    {
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(
                nameof(ItemHeight),
                typeof(double),
                typeof(QuickListPresenter),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty ItemsSourceProperty =
            ItemsControl.ItemsSourceProperty.AddOwner(
                typeof(QuickListPresenter),
                new FrameworkPropertyMetadata(HandleItemsSourceChanged));

        public static readonly DependencyProperty ItemContainerTypeProperty =
            DependencyProperty.Register(
                nameof(ItemContainerType),
                typeof(Type),
                typeof(QuickListPresenter),
                new FrameworkPropertyMetadata(HandleItemContainerTypeChanged));

        int offset;

        public QuickListPresenter()
        {
        }

        public Type ItemContainerType
        {
            get { return (Type)GetValue(ItemContainerTypeProperty); }
            set { SetValue(ItemContainerTypeProperty, value); }
        }

        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        public IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        bool IScrollInfo.CanVerticallyScroll
        {
            get { return true; }
            set { }
        }

        bool IScrollInfo.CanHorizontallyScroll
        {
            get { return false; }
            set { }
        }

        double IScrollInfo.ExtentWidth => ActualWidth;
        double IScrollInfo.ExtentHeight => ItemsSource?.Count ?? 0;
        double IScrollInfo.ViewportWidth => ActualWidth;
        double IScrollInfo.ViewportHeight => Children.Count;
        double IScrollInfo.HorizontalOffset => 0;
        double IScrollInfo.VerticalOffset => offset;
        ScrollViewer IScrollInfo.ScrollOwner { get; set; }

        public void SetVerticalOffset(double offset)
        {
            var max = ItemsSource?.Count ?? 0 - Children.Count;
            this.offset = (int)Math.Max(0, Math.Min(offset, max));
            AssignContainers();
            InvalidateScrollInfo();
        }

        void IScrollInfo.LineUp() => SetVerticalOffset(offset - 1);
        void IScrollInfo.LineDown() => SetVerticalOffset(offset + 1);
        void IScrollInfo.PageUp() => SetVerticalOffset(offset - Children.Count);
        void IScrollInfo.PageDown() => SetVerticalOffset(offset + Children.Count);
        void IScrollInfo.MouseWheelUp() => SetVerticalOffset(offset - 1);
        void IScrollInfo.MouseWheelDown() => SetVerticalOffset(offset + 1);
        void IScrollInfo.LineLeft() { }
        void IScrollInfo.LineRight() { }
        void IScrollInfo.PageLeft() { }
        void IScrollInfo.PageRight() { }
        void IScrollInfo.MouseWheelLeft() { }
        void IScrollInfo.MouseWheelRight() { }
        void IScrollInfo.SetHorizontalOffset(double offset) { }
        Rect IScrollInfo.MakeVisible(Visual visual, Rect rectangle) => Rect.Empty;

        protected override Size MeasureOverride(Size availableSize)
        {
            var count = ItemsSource?.Count ?? 0;
            return new Size(
                availableSize.Width,
                Math.Min(count * ItemHeight, availableSize.Height));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            CreateContainers(finalSize.Height);
            PositionContainers(finalSize);
            AssignContainers();
            return finalSize;
        }

        void CreateContainers(double height)
        {
            if (ItemContainerType != null)
            {
                var count = (int)Math.Min(
                    Math.Ceiling(height / ItemHeight),
                    ItemsSource?.Count ?? 0);

                while (InternalChildren.Count < count)
                {
                    var container = (FrameworkElement)Activator.CreateInstance(ItemContainerType);
                    InternalChildren.Add(container);
                }

                if (InternalChildren.Count > count)
                {
                    InternalChildren.RemoveRange(count, InternalChildren.Count - count);
                }
            }
        }

        void PositionContainers(Size size)
        {
            var y = 0.0;

            foreach (FrameworkElement child in InternalChildren)
            {
                child.Arrange(new Rect(0, y, size.Width, ItemHeight));
                y += ItemHeight;
            }
        }

        void AssignContainers()
        {
            if (ItemsSource != null)
            {
                var i = offset;

                foreach (var child in InternalChildren)
                {
                    ((FrameworkElement)child).DataContext = ItemsSource[i++];
                }
            }
        }

        void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Reset:
                    InvalidateMeasure();
                    break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                    AssignContainers();
                    break;
            }

            InvalidateScrollInfo();
        }

        void ItemsSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            var oldIncc = e.OldValue as INotifyCollectionChanged;
            var newIncc = e.NewValue as INotifyCollectionChanged;

            if (oldIncc != null)
            {
                oldIncc.CollectionChanged += CollectionChanged;
            }

            if (newIncc != null)
            {
                newIncc.CollectionChanged += CollectionChanged;
            }

            InvalidateMeasure();
            InvalidateScrollInfo();
        }

        void ItemContainerTypeChanged()
        {
            InternalChildren.Clear();
            CreateContainers(ActualHeight);
        }

        void InvalidateScrollInfo()
        {
            ((IScrollInfo)this).ScrollOwner?.InvalidateScrollInfo();
        }

        static void HandleItemContainerTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as QuickListPresenter)?.ItemContainerTypeChanged();
        }

        static void HandleItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as QuickListPresenter)?.ItemsSourceChanged(e);
        }
    }
}