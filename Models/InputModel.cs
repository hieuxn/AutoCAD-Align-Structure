using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCAD_Align_Structure.Models
{
	public class InputModel
	{
		private static int _defaultSpanCount = 5;
		private static double _defaultProtrusionLength = 5000; // mm
		private static double _defaultEmbedmentLength = 2000; // mm

		private int _spanCount = _defaultSpanCount;
		private double _protrusionLength = _defaultProtrusionLength;
		private double _embedmentLength = _defaultEmbedmentLength;

		public int SpanCount
		{
			get => _spanCount;
			set
			{
				_spanCount = value;
				_defaultSpanCount = value;
			}
		}

		public double ProtrusionLength
		{
			get => _protrusionLength;
			set
			{
				_protrusionLength = value;
				_defaultProtrusionLength = value;
			}
		}

		public double EmbedmentLength
		{
			get => _embedmentLength;
			set
			{
				_embedmentLength = value;
				_defaultEmbedmentLength = value;
			}
		}

		public InputModel Clone()
		{
			return new InputModel
			{
				SpanCount = this.SpanCount,
				ProtrusionLength = this.ProtrusionLength,
				EmbedmentLength = this.EmbedmentLength
			};
		}
	}

}
