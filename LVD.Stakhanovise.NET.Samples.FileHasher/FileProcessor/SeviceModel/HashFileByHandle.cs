using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor.SeviceModel
{
	public class HashFileByHandle
	{
		public HashFileByHandle( Guid handleId )
		{
			HandleId = handleId;
		}

		public Guid HandleId { get; set; }
	}
}
