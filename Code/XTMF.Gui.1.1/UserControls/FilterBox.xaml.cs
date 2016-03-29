﻿/*
    Copyright 2014-2016 Travel Modelling Group, Department of Civil Engineering, University of Toronto

    This file is part of XTMF.

    XTMF is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    XTMF is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with XTMF.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace XTMF.Gui
{
    /// <summary>
    /// Interaction logic for FilterBox.xaml
    /// </summary>
    public partial class FilterBox : UserControl
    {
        private ICollectionView ItemsSource;
        public FilterBox()
        {
            UseItemSourceFilter = true;
            InitializeComponent();
        }
        public static readonly DependencyProperty FilterWatermarkProperty = DependencyProperty.Register("FilterWatermark", typeof(string), typeof(FilterBox),
    new FrameworkPropertyMetadata("Search...", FrameworkPropertyMetadataOptions.AffectsRender, OnFilterWatermarkChanged));

        private static void OnFilterWatermarkChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            ((FilterBox)source).Box.HintText = e.NewValue as string;
        }

        public string FilterWatermark
        {
            get { return GetValue(FilterWatermarkProperty) as string; }
            set { SetValue(FilterWatermarkProperty, value); }
        }

        public bool UseItemSourceFilter { get; set; }

        private Action Refresh;
        private Func<object, string, bool> _Filter;

        public event EventHandler EnterPressed;


        public Func<object, string, bool> Filter
        {
            get
            {
                return _Filter;
            }
            set
            {
                _Filter = value;
                if (_Display != null && ItemsSource != null)
                {
                    if (UseItemSourceFilter)
                    {
                        if (!ItemsSource.CanFilter)
                        {
                            throw new NotSupportedException("The FilterBox is unable to filter data  of type " + ItemsSource.SourceCollection.GetType().FullName);
                        }

                        ItemsSource.Filter = new Predicate<object>((o) => _Filter(o, CurrentBoxText));
                    }
                }
            }
        }

        private ItemsControl _Display;
        public ItemsControl Display
        {
            set
            {
                _Display = value;
                ItemsSource = CollectionViewSource.GetDefaultView(value.ItemsSource);
                if (_Filter != null)
                {
                    Filter = _Filter;
                }
                Refresh = () =>
                {
                    if (UseItemSourceFilter)
                    {
                        ItemsSource.Refresh();
                    }
                    else
                    {
                        var items = ItemsSource.GetEnumerator();
                        using (var differ = ItemsSource.DeferRefresh())
                        {
                            while (items.MoveNext())
                            {
                                Filter(items.Current, Box.Text);
                            }
                        }
                    }
                };
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            Box.Focus();
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (e.Handled == false)
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        e.Handled = ClearFilter();
                        break;
                    case Key.Enter:
                        e.Handled = HandleEnterPress();
                        break;
                }

            }
            base.OnKeyDown(e);
        }

        private bool HandleEnterPress()
        {
            var ev = EnterPressed;
            if (ev != null)
            {
                ev(this, new EventArgs());
                return true;
            }
            return false;
        }

        private bool ClearFilter()
        {
            if (!string.IsNullOrWhiteSpace(Box.Text))
            {
                Box.Text = string.Empty;
                return true;
            }
            return false;
        }

        private void ClearFilter_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ClearFilter();
        }

        private string CurrentBoxText = String.Empty;

        private void Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            CurrentBoxText = Box.Text;
            RefreshFilter();
            ClearFilterButton.Visibility = !string.IsNullOrWhiteSpace(Box.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        internal void RefreshFilter()
        {
            if (Refresh != null)
            {
                Dispatcher.BeginInvoke(Refresh, DispatcherPriority.Input);
            }
        }
    }
}
