using System.IO;

namespace Subro.IO
{
	public interface IContentWriter
	{
		void WriteContents(BinaryWriter w);
	}
}
