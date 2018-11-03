using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uMod.Utilities
{
    public class FacePunchTextTable
    {
        private class Row
        {
            public string[] values;

            public Row(string[] values)
            {
                this.values = values;
            }
        }

        private class Column
        {
            public string title;

            public int width;

            public Column(string title)
            {
                this.title = title;
                width = title.Length;
            }
        }

        private List<Row> rows = new List<Row>();

        private List<Column> columns = new List<Column>();

        private StringBuilder builder = new StringBuilder();

        private string text = string.Empty;

        private bool dirty;

        public void Clear()
        {
            rows.Clear();
            columns.Clear();
            dirty = true;
        }

        public void AddColumns(params string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                columns.Add(new Column(values[i]));
            }
            dirty = true;
        }

        public void AddColumn(string title)
        {
            columns.Add(new Column(title));
            dirty = true;
        }

        public void AddRow(params string[] values)
        {
            int num = Math.Min(columns.Count, values.Length);
            for (int i = 0; i < num; i++)
            {
                columns[i].width = Math.Max(columns[i].width, values[i].Length);
            }
            rows.Add(new Row(values));
            dirty = true;
        }

        public void AppendLine(string line)
        {
            builder.AppendLine(line);
            dirty = true;
        }

        public override string ToString()
        {
            if (dirty)
            {
                //net 3.5 workaround instead of StringBuilder.Clear()
                builder.Length = 0;
                for (int i = 0; i < columns.Count; i++)
                {
                    builder.Append(columns[i].title.PadRight(columns[i].width + 1));
                }
                builder.AppendLine();
                for (int j = 0; j < rows.Count; j++)
                {
                    Row row = rows[j];
                    int num = Math.Min(columns.Count, row.values.Length);
                    for (int k = 0; k < num; k++)
                    {
                        builder.Append(row.values[k].PadRight(columns[k].width + 1));
                    }
                    builder.AppendLine();
                }
                text = builder.ToString();
                dirty = false;
            }
            return text;
        }
    }
}
