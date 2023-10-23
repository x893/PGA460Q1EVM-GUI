using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Subro.IO;

namespace Subro.Controls
{
	[Serializable]
	public class GroupingOptions : INotifyPropertyChanged, IEquatable<GroupingOptions>, ISerializable, ICustomSerializer
	{
		public GroupingOptions()
		{
			add<bool>(GroupingOption.StartCollapsed, false);
			add<SortOrder>(GroupingOption.GroupSortOrder, SortOrder.Ascending);
			add<bool>(GroupingOption.AlwaysGroupOnText, false);
			add<bool>(GroupingOption.ShowCount, true);
			add<bool>(GroupingOption.ShowGroupName, true);
			add<bool>(GroupingOption.SelectRowsOnDoubleClick, true);
		}

		private void add<T>(GroupingOption o, T Default)
		{
			m_list.Add(new GroupingOptionValue<T>(Default, o)
			{
				Owner = this
			});
		}

		public GroupingOptionValue this[GroupingOption option]
		{
			get
			{
				for (int i = 0; i < m_list.Count; i++)
				{
					if (m_list[i].Option == option)
					{
						return m_list[i];
					}
				}
				return null;
			}
		}

		private T GetValue<T>(GroupingOption o)
		{
			return ((GroupingOptionValue<T>)this[o]).Value;
		}

		private void SetValue<T>(GroupingOption o, T value)
		{
			((GroupingOptionValue<T>)this[o]).Value = value;
		}

		private bool ShouldSerialize(GroupingOption o)
		{
			return !this[o].IsDefault;
		}

		public bool StartCollapsed
		{
			get
			{
				return GetValue<bool>(GroupingOption.StartCollapsed);
			}
			set
			{
				SetValue<bool>(GroupingOption.StartCollapsed, value);
			}
		}

		private bool ShouldSerializeStartCollapsed()
		{
			return ShouldSerialize(GroupingOption.StartCollapsed);
		}

		[DefaultValue(SortOrder.Ascending)]
		public SortOrder GroupSortOrder
		{
			get
			{
				return GetValue<SortOrder>(GroupingOption.GroupSortOrder);
			}
			set
			{
				SetValue<SortOrder>(GroupingOption.GroupSortOrder, value);
			}
		}

		private bool ShouldSerializeGroupSortOrder()
		{
			return ShouldSerialize(GroupingOption.GroupSortOrder);
		}

		public bool AlwaysGroupOnText
		{
			get
			{
				return GetValue<bool>(GroupingOption.AlwaysGroupOnText);
			}
			set
			{
				SetValue<bool>(GroupingOption.AlwaysGroupOnText, value);
			}
		}

		private bool ShouldSerializeAlwaysGroupOnText()
		{
			return ShouldSerialize(GroupingOption.AlwaysGroupOnText);
		}

		public bool ShowCount
		{
			get
			{
				return GetValue<bool>(GroupingOption.ShowCount);
			}
			set
			{
				SetValue<bool>(GroupingOption.ShowCount, value);
			}
		}

		private bool ShouldSerializeShowCount()
		{
			return ShouldSerialize(GroupingOption.ShowCount);
		}

		public bool ShowGroupName
		{
			get
			{
				return GetValue<bool>(GroupingOption.ShowGroupName);
			}
			set
			{
				SetValue<bool>(GroupingOption.ShowGroupName, value);
			}
		}

		private bool ShouldSerializeShowGroupName()
		{
			return ShouldSerialize(GroupingOption.ShowGroupName);
		}

		public bool SelectRowsOnDoubleClick
		{
			get
			{
				return GetValue<bool>(GroupingOption.SelectRowsOnDoubleClick);
			}
			set
			{
				SetValue<bool>(GroupingOption.SelectRowsOnDoubleClick, value);
			}
		}

		private bool ShouldSerializeSelectRowsOnDoubleClick()
		{
			return ShouldSerialize(GroupingOption.SelectRowsOnDoubleClick);
		}

		public bool HasNonDefaultValues
		{
			get
			{
				foreach (GroupingOptionValue groupingOptionValue in m_list)
				{
					if (!groupingOptionValue.IsDefault)
					{
						return true;
					}
				}
				return false;
			}
		}

		public void CopyValues(GroupingOptions options)
		{
			foreach (GroupingOptionValue groupingOptionValue in m_list)
			{
				groupingOptionValue.CopyValue(options[groupingOptionValue.Option]);
			}
		}

		public void SetDefaults()
		{
			foreach (GroupingOptionValue groupingOptionValue in m_list)
			{
				groupingOptionValue.Reset();
			}
		}

		internal void NotifyChanged(GroupingOption o)
		{
			if (OptionChanged != null)
			{
				OptionChanged(this, new GroupingOptionChangedEventArgs(o));
			}
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(o.ToString()));
			}
		}

		[field: NonSerialized]
		public event PropertyChangedEventHandler PropertyChanged;

		[field: NonSerialized]
		public event EventHandler<GroupingOptionChangedEventArgs> OptionChanged;

		public bool Equals(GroupingOptions o)
		{
			bool result;
			if (o == null)
			{
				result = false;
			}
			else if (o.m_list.Count != m_list.Count)
			{
				result = false;
			}
			else
			{
				foreach (GroupingOptionValue groupingOptionValue in m_list)
				{
					if (!groupingOptionValue.Equals(o[groupingOptionValue.Option]))
					{
						return false;
					}
				}
				result = true;
			}
			return result;
		}

		private GroupingOptions(SerializationInfo info, StreamingContext context) : this()
		{
			foreach (SerializationEntry serializationEntry in info)
			{
				try
				{
					GroupingOption option = EnumFunctions.Parse<GroupingOption>(serializationEntry.Name);
					this[option].SetValue(serializationEntry.Value);
				}
				catch
				{
				}
			}
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			foreach (GroupingOptionValue groupingOptionValue in m_list)
			{
				if (!groupingOptionValue.IsDefault)
				{
					info.AddValue(groupingOptionValue.Option.ToString(), groupingOptionValue.GetValue());
				}
			}
		}

		bool ICustomSerializer.Serialize(SimpleObjectSerializer s)
		{
			GroupingOptionValue[] array = (from g in m_list
			where !g.IsDefault
			select g).ToArray<GroupingOptionValue>();
			s.Writer.Write((byte)array.Length);
			if (array.Length > 0)
			{
				foreach (GroupingOptionValue groupingOptionValue in array)
				{
					s.Writer.Write(groupingOptionValue.Option.ToString());
					s.WriteSubValue(groupingOptionValue.GetValue());
				}
			}
			return true;
		}

		bool ICustomSerializer.Deserialize(SimpleObjectDeserializer ds)
		{
			int num = (int)ds.Reader.ReadByte();
			for (int i = 0; i < num; i++)
			{
				string value = ds.Reader.ReadString();
				object subValue = ds.GetSubValue();
				GroupingOption option = EnumFunctions.Parse<GroupingOption>(value);
				this[option].SetValue(subValue);
			}
			return true;
		}

		bool ICustomSerializer.Initialize(SimpleObjectSerializer s)
		{
			return true;
		}

		public const SortOrder DefaultGroupSortOrder = SortOrder.Ascending;
		private List<GroupingOptionValue> m_list = new List<GroupingOptionValue>();
	}
}
