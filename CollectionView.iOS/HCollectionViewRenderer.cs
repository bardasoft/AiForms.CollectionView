﻿using System;
using AiForms.Renderers;
using AiForms.Renderers.iOS;
using Xamarin.Forms;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using CoreGraphics;
using AiForms.Renderers.iOS.Cells;
using System.ComponentModel;
using System.Collections.Specialized;
using Xamarin.Forms.Internals;

[assembly: ExportRenderer(typeof(HCollectionView), typeof(HCollectionViewRenderer))]
namespace AiForms.Renderers.iOS
{
    [Foundation.Preserve(AllMembers = true)]
    public class HCollectionViewRenderer : CollectionViewRenderer
    {
        UICollectionView _collectionView;
        CGRect _previousFrame = CGRect.Empty;
        bool _disposed;
        HCollectionView _hCollectionView => Element as HCollectionView;

        protected override void OnElementChanged(ElementChangedEventArgs<CollectionView> e)
        {
            if (e.NewElement != null)
            {
                ViewLayout = new UICollectionViewFlowLayout();
                ViewLayout.ScrollDirection = UICollectionViewScrollDirection.Horizontal;
                ViewLayout.SectionInset = new UIEdgeInsets(0, 0, 0, 0);
                ViewLayout.MinimumLineSpacing = 0.0f;
                ViewLayout.MinimumInteritemSpacing = 0.0f;

                _collectionView = new UICollectionView(CGRect.Empty, ViewLayout);
                _collectionView.RegisterClassForCell(typeof(ContentCellContainer), typeof(ContentCell).FullName);
                _collectionView.RegisterClassForSupplementaryView(typeof(ContentCellContainer), UICollectionElementKindSection.Header, SectionHeaderId);

                _collectionView.ShowsHorizontalScrollIndicator = false;
                _collectionView.AllowsSelection = true;

                SetNativeControl(_collectionView);


                DataSource = new HCollectionViewSource(e.NewElement, _collectionView);
                _collectionView.Source = DataSource;

                UpdateSpacing();
            }

            base.OnElementChanged(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                DataSource?.Dispose();
                DataSource = null;

                _collectionView?.Dispose();
                _collectionView = null;
            }
            _disposed = true;
            base.Dispose(disposing);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            if (_previousFrame != Frame)
            {
                UpdateCellSize();
                UpdateGroupHeaderWidth();
                ViewLayout.InvalidateLayout();
            }
            _previousFrame = Frame;
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == HCollectionView.ColumnWidthProperty.PropertyName ||
               e.PropertyName == VisualElement.HeightRequestProperty.PropertyName)
            {
                UpdateCellSize();
                ViewLayout.InvalidateLayout();

            }
            else if (e.PropertyName == HCollectionView.GroupHeaderWidthProperty.PropertyName)
            {
                UpdateGroupHeaderWidth();
                ViewLayout.InvalidateLayout();
            }
            else if (e.PropertyName == HCollectionView.SpacingProperty.PropertyName)
            {
                UpdateSpacing();
                ViewLayout.InvalidateLayout();
            }
            else if (e.PropertyName == HCollectionView.IsInfiniteProperty.PropertyName)
            {
                _collectionView.ReloadData();
                ViewLayout.InvalidateLayout();
            }
        }

        protected override UICollectionViewScrollPosition GetScrollPosition(ScrollToPosition position)
        {
            switch (position)
            {
                case ScrollToPosition.Center:
                    return UICollectionViewScrollPosition.CenteredHorizontally;
                case ScrollToPosition.End:
                    return UICollectionViewScrollPosition.Right;
                case ScrollToPosition.Start:
                    return UICollectionViewScrollPosition.Left;
                case ScrollToPosition.MakeVisible:
                default:
                    return UICollectionViewScrollPosition.None;
            }
        }

        protected override void OnGroupedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_hCollectionView.IsInfinite)
            {
                // If infinite, any events are dealt with as a reset event.
                UpdateItems(e, 0, true);
                return;
            }

            var til = (TemplatedItemsList<ItemsView<Cell>, Cell>)sender;
            // When scroll orientation is horizontal, the groupIndex can't be correctly got using 
            // "templatedItems.IndexOf(til.HeaderContent)" for some reason.
            // So BindingContext instead of HeaderContent is used and got the group index.
            var groupIndex = TemplatedItemsView.TemplatedItems.ListProxy.IndexOf(til.BindingContext);

            UpdateItems(e, groupIndex, false);
        }

        protected override void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(_hCollectionView.IsInfinite)
            {
                UpdateItems(e, 0, true, true);
                return;
            }
            base.OnCollectionChanged(sender, e);
        }

        void UpdateCellSize()
        {
            if (Element.Height < 0)
            {
                return;
            }
            var height = Element.HeightRequest >= 0 ? Element.HeightRequest : Bounds.Height;
            DataSource.CellSize = new CGSize((float)_hCollectionView.ColumnWidth, (float)height);
        }

        void UpdateSpacing()
        {
            ViewLayout.MinimumLineSpacing = (System.nfloat)_hCollectionView.Spacing;
        }

        void UpdateGroupHeaderWidth()
        {
            if (_hCollectionView.IsGroupingEnabled)
            {
                // TODO: 細かいサイズ調整をする場合はサブクラスで対応する
                ViewLayout.HeaderReferenceSize = new CGSize(_hCollectionView.GroupHeaderWidth, Bounds.Height);
            }
        }

    }
}
