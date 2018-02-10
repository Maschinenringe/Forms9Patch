﻿using Android.Views;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Android.Widget;
using Android.Runtime;

[assembly: ExportRenderer(typeof(Forms9Patch.EnhancedListView), typeof(Forms9Patch.Droid.EnhancedListViewRenderer))]
namespace Forms9Patch.Droid
{
    public class EnhancedListViewRenderer : Xamarin.Forms.Platform.Android.ListViewRenderer
    {

        ScrollListener _scrollListener;
        ScrollListener ScrollListener
        {
            get
            {
                _scrollListener = _scrollListener ?? new ScrollListener();
                return _scrollListener;
            }
        }


        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.ListView> e)
        {
            base.OnElementChanged(e);
            if (e.OldElement is EnhancedListView oldElement)
            {
                oldElement.RendererScrollBy -= ScrollBy;
                oldElement.RendererScrollTo -= ScrollTo;
                oldElement.RendererScrollOffset -= ScrollOffset;
                oldElement.RendererHeaderHeight -= HeaderHeight;
                ScrollListener.Element = null;
            }
            if (e.NewElement is EnhancedListView newElement)
            {
                newElement.RendererScrollBy += ScrollBy;
                newElement.RendererScrollTo += ScrollTo;
                newElement.RendererScrollOffset += ScrollOffset;
                newElement.RendererHeaderHeight += HeaderHeight;

                ScrollListener.Element = newElement;
                Control.SetOnScrollListener(ScrollListener);

            }
            Control.Divider = null;
            Control.DividerHeight = 0;
            /* none of the following seem to do anything!
			Control.SetSelector(Android.Resource.Color.Transparent);
			Control.ChoiceMode = ChoiceMode.MultipleModal;
			Control.SetMultiChoiceModeListener(new ChoiceListener());
			*/
            //Control.Enabled = false;  // disables selection AND scrolling!
            Control.Clickable = false;  // appears to do nothing?
        }

        //int offset;
        //int lastIndex = -1;
        bool ScrollBy(double delta, bool animated)
        {
            if (animated)
            {
                Control.SmoothScrollByOffset((int)delta);
                return true;
            }
            int index = Control.FirstVisiblePosition;
            var cellView = Control.GetChildAt(index);
            var offset = (cellView == null) ? 0 : (cellView.Top - Control.ListPaddingTop);
            offset += (int)(delta * Settings.Context.Resources.DisplayMetrics.Density);
            Control.SetSelectionFromTop(index, -offset);
            return true;
        }

        bool ScrollTo(double offset, bool animated)
        {
            if (Element is EnhancedListView listView && listView.ItemsSource is GroupWrapper groupWrapper)
            {
                // Animation not supported on Android because SmoothScrollToTop fires a bunch of ScrollListener.OnScrolledStateChanged(ScrolledState.Idle) calls during animation.
                // There appears to be no way of knowing when the animation is on going without overriding the native Android.ListView's LinearLayoutManager.  Too tall of an order.
                Control.SetSelectionFromTop(0, (int)Math.Round(-offset * Forms9Patch.Display.Scale));
                return true;
            }
            return false;
        }

        double ScrollOffset()
        {
            if (Element is EnhancedListView listView && listView.ItemsSource is GroupWrapper groupWrapper)
            {
                var cellView = Control.GetChildAt(0);
                if (cellView == null)
                    return -1;

                int index = Control.FirstVisiblePosition;
                if (index == 0)
                    return -cellView.Top / Forms9Patch.Display.Scale;

                if (Control.HeaderViewsCount > 0)
                {
                    index--;
                    if (Control.HeaderViewsCount > 1)
                        throw new NotSupportedException("EnhancedListViewRenderer only supports 0 or 1 ListView.Header");
                }

                var deepDataSource = groupWrapper.TwoDeepDataSetForFlatIndex(index);
                return HeaderHeight() + deepDataSource.Offset - cellView.Top / Forms9Patch.Display.Scale;
            }
            return 0;
        }

        double NativeHeaderHeight()
        {
            if (Element.HeaderElement is VisualElement headerElement)
            {
                if (Platform.GetRenderer(headerElement) is IVisualElementRenderer headerRenderer)
                    return headerRenderer.View.Height;
                throw new Exception("Element.HeaderElement should have a renderer");
            }
            return 0;
        }

        double HeaderHeight() => NativeHeaderHeight() / Forms9Patch.Display.Scale;


        double NativeFooterHeight()
        {
            if (Element.FooterElement is VisualElement footerElement)
            {
                if (Platform.GetRenderer(footerElement) is IVisualElementRenderer footerRenderer)
                    return footerRenderer.View.Height;
                throw new Exception("Element.HeaderElement should have a renderer");
            }
            return 0;
        }

        double FooterHeight() => NativeFooterHeight() / Forms9Patch.Display.Scale;
    }


    class ScrollListener : Java.Lang.Object, Android.Widget.ListView.IOnScrollListener
    {
        public Forms9Patch.EnhancedListView Element;

        public void OnScroll(AbsListView view, int firstVisibleItem, int visibleItemCount, int totalItemCount) => Element?.OnScrolling(this, EventArgs.Empty);

        public void OnScrollStateChanged(AbsListView view, [GeneratedEnum] ScrollState scrollState)
        {
            if (scrollState == ScrollState.Idle)
                Element?.OnScrolled(this, EventArgs.Empty);
        }
    }

}