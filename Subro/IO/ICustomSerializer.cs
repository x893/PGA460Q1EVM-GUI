namespace Subro.IO
{
	public interface ICustomSerializer
	{
		bool Initialize(SimpleObjectSerializer serializer);
		bool Serialize(SimpleObjectSerializer serializer);
		bool Deserialize(SimpleObjectDeserializer deserializer);
	}
}
