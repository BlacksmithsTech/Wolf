using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Generation
{
	internal class IndentableStringBuilder
	{
		private StringBuilder _builder = new StringBuilder();
		private bool indentationRequired = true;

		internal short Indentation { get; set; }

		internal IndentableStringBuilder Append(object o)
		{
			if (null != o)
			{
				if (indentationRequired)
				{
					indentationRequired = false;
					this._builder.Append('\t', this.Indentation);
				}

				var value = o.ToString();
				if(value.EndsWith(Environment.NewLine))
				{
					indentationRequired = true;
					value = value.Remove(value.Length - Environment.NewLine.Length);
					
				}

				value = value.Replace(Environment.NewLine, $"{Environment.NewLine}{new string('\t', this.Indentation)}");

				if(indentationRequired)
					value = value.Insert(value.Length, Environment.NewLine);

				this._builder.Append(value);

			}

			return this;
		}

		internal IndentableStringBuilder Append(params object[] objects)
		{
			foreach (var o in objects)
				this.Append(o);
			return this;
		}

		internal IndentableStringBuilder AppendLine()
		{
			return this.Append($"{Environment.NewLine}");
		}

		internal IndentableStringBuilder AppendLine(object o)
		{
			return this.Append($"{o}{Environment.NewLine}");
		}

		internal IndentableStringBuilder AppendFormat(string f, params object[] objects)
		{
			return this.Append(string.Format(f, objects));
		}

		internal void Indent()
		{
			this.Indentation++;
		}

		internal void Outdent()
		{
			this.Indentation = (short)Math.Max(this.Indentation - 1, 0);
		}

		public override string ToString()
		{
			return this._builder.ToString();
		}
	}
}
