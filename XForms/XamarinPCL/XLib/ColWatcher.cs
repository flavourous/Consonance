using LibSharpHelp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XLib
{
    public class ColWatcher
    {
        readonly Dictionary<Object, double?> valid_desired_widths = new Dictionary<Object, double?>();
        readonly List<ColumnDefinition> columns = new List<ColumnDefinition>();
        readonly Dictionary<View, ColumnDefinition> index = new Dictionary<View, ColumnDefinition>();
        readonly Dictionary<ColumnDefinition, List<View>> reverse_index = new Dictionary<ColumnDefinition, List<View>>();
        readonly Func<double> getavail;

        public ColWatcher(Func<double> getavail)
        {
            this.getavail = getavail;
        }

        public ColumnDefinition AddColumn(ColumnDefinition cd)
        {
            columns.Add(cd);
            reverse_index[cd] = new List<View>();
            possibly_invalidated[cd] = true;
            valid_desired_widths[cd] = 0.0;
            return cd;
        }

        public View AddCell(View cell, int col)
        {
            var c = columns[col];
            index[cell] = c;
            reverse_index[c].Add(cell);
            cell.MeasureInvalidated += Cell_MeasureInvalidated;
            Cell_MeasureInvalidated(cell, null);
            return cell;
        }

        public void RemoveCell(View cell)
        {
            var c = index[cell];
            reverse_index[c].Remove(cell);
            index.Remove(cell);
            cell.MeasureInvalidated -= Cell_MeasureInvalidated;

            var cw = valid_desired_widths[c] ?? 0.0;
            var vw = valid_desired_widths[cell] ?? 0.0;

            if (vw >= cw)
            {
                // this forces a re-assesmnet. otherwise it latches to largest original.
                valid_desired_widths[c] = 0.0; 
                possibly_invalidated[c] = true;
            }

            valid_desired_widths.Remove(cell);
        }

        private void Cell_MeasureInvalidated(object sender, EventArgs e)
        {
            // record validity.
            valid_desired_widths[sender] = null;
            possibly_invalidated[index[sender as View]] = true;
        }

        

        Dictionary<ColumnDefinition, bool> possibly_invalidated = new Dictionary<ColumnDefinition, bool>();
        public void LayoutPass(bool totallyInvalidate = false)
        {
            if(totallyInvalidate)
                foreach (var c in columns)
                    possibly_invalidated[c] = true;
            // find columns with invalidated desired widths, and recalculate them.
            Dictionary<ColumnDefinition, bool> failures = new Dictionary<ColumnDefinition, bool>();
            foreach (var pi in possibly_invalidated.Keys)
            {
                var val = possibly_invalidated[pi];
                if (val)
                {
                    // this one might be wrong. lets see.
                    double biggest_new = 0.0;
                    foreach (var v in reverse_index[pi])
                    {
                        if (!valid_desired_widths[v].HasValue)
                        {
                            double nv = v.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity).Request.Width;
                            biggest_new = Math.Max(biggest_new, nv);
                            if (nv > 0) valid_desired_widths[v] = nv;
                            else failures[pi] = true;
                        }
                    }
                    if (biggest_new > (valid_desired_widths[pi] ?? 0.0))
                        valid_desired_widths[pi] = biggest_new;
                }
            }
            possibly_invalidated = failures;

            // now we distribute the column widths
            double distributed = 0;
            Action<ColumnDefinition, double> setcol = (c, w) =>
            {
                var use = Math.Max(0, w);
                c.Width = new GridLength(use);
                distributed += use;
            };

            // if we've smaller than container in total of cols, they can all have thier cake.
            // otherwise, the minimum a col should get is an equal share, and the rest should be
            // distributed among the remaining cols, weighted upon thier desired width.
            var desired = (from c in columns select valid_desired_widths[c].Value).ToArray();
            double avail = getavail();
            double destot = desired.Sum();
            if (destot <= avail)
                columns.Both(desired, setcol);
            else
            {
                double min = avail / columns.Count;
                double share = (from d in desired select Math.Min(min, d)).Sum();
                avail -= share;
                destot -= share;
                columns.Both(desired, (col, des) => {
                    if (des <= min) setcol(col, des);
                    else
                    {
                        double weight = (des - min) / destot;
                        setcol(col, weight * avail + min);
                    }
                });
            }
        }

        public void Clear()
        {
            foreach (var v in index.Keys)
                v.MeasureInvalidated -= Cell_MeasureInvalidated;
            columns.Clear();
            index.Clear();
            reverse_index.Clear();
            valid_desired_widths.Clear();
            possibly_invalidated.Clear();
        }
    }
}
