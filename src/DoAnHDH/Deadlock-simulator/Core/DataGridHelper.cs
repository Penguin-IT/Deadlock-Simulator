using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Specialized;

namespace Deadlock_simulator.Core
{
    public static class DataGridHelper
    {
        public static ObservableCollection<DataGridColumn> GetBindableColumns(DependencyObject obj)
        {
            return (ObservableCollection<DataGridColumn>)obj.GetValue(BindableColumnsProperty);
        }

        public static void SetBindableColumns(DependencyObject obj, ObservableCollection<DataGridColumn> value)
        {
            obj.SetValue(BindableColumnsProperty, value);
        }

        public static readonly DependencyProperty BindableColumnsProperty =
            DependencyProperty.RegisterAttached(
                "BindableColumns",
                typeof(ObservableCollection<DataGridColumn>),
                typeof(DataGridHelper),
                new PropertyMetadata(null, OnColumnsChanged));

        private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid grid) return;

            grid.Columns.Clear();

            if (e.NewValue is ObservableCollection<DataGridColumn> columns)
            {
               
                foreach (var col in columns)
                {
                    grid.Columns.Add(col);
                }

               
                columns.CollectionChanged += (s, ev) =>
                {
                    if (ev.Action == NotifyCollectionChangedAction.Reset)
                    {
                        grid.Columns.Clear();
                    }

                    if (ev.NewItems != null)
                    {
                        foreach (DataGridColumn col in ev.NewItems)
                        {
                            grid.Columns.Add(col);
                        }
                    }

                    if (ev.OldItems != null)
                    {
                        foreach (DataGridColumn col in ev.OldItems)
                        {
                            grid.Columns.Remove(col);
                        }
                    }
                };
            }
        }
    }
}
