using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fusumity.Attributes.Specific;
using UnityEngine;

namespace Fusumity.Collections
{
	[Serializable]
	public class ReorderableEnumArray<TEnum> : ReorderableEnumArray<TEnum, EmptyStruct> where TEnum : unmanaged, Enum {}

	[Serializable]
	public class ReorderableEnumArray<TEnum, TValue> : ReorderableEnumArray<TEnum, TValue, EnumValue<TEnum, TValue>>
		where TEnum : unmanaged, Enum {}

	[Serializable]
	public class ReorderableEnumReferenceArray<TEnum, TValue> : ReorderableEnumArray<TEnum, TValue, EnumReferenceValue<TEnum, TValue>>
		where TEnum : unmanaged, Enum
		where TValue : class {}

	[Serializable]
	public class ReorderableEnumArray<TEnum, TValue, TEnumValue> : EnumArray<TEnum, TValue, TEnumValue>
		where TEnum : unmanaged, Enum
		where TEnumValue : IEnumValue<TEnum>, new()
	{
		protected override bool IsReorderable => true;

		[SerializeField, HideInInspector]
		private int[] _indexes;

		public unsafe ref TEnumValue this[TEnum enumValue]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref values[GetIndexOf(enumValue)];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe int GetIndexOf(TEnum enumValue)
		{
			return _indexes[*(int*)(&enumValue)];
		}

#if UNITY_EDITOR
		protected override void LazyInitialize()
		{
			base.LazyInitialize();
			if (_indexes != null)
				return;

			_indexes = new int[values.Length];
		}

		protected override void OnValuesUpdated()
		{
			if (_indexes.Length != values.Length)
				_indexes = new int[values.Length];

			FillIndexes();
		}

		private unsafe void FillIndexes()
		{
			for (var i = 0; i < values.Length; i++)
			{
				var enumValue = values[i].EnumValue;
				_indexes[*(int*)(&enumValue)] = i;
			}
		}
#endif
	}

	[Serializable]
	public class EnumArray<TEnum> : EnumArray<TEnum, EmptyStruct> where TEnum : unmanaged, Enum {}

	[Serializable]
	public class EnumArray<TEnum, TValue> : OrderedEnumArray<TEnum, TValue, EnumValue<TEnum, TValue>>
		where TEnum : unmanaged, Enum {}

	[Serializable]
	public class EnumReferenceArray<TEnum, TValue> : OrderedEnumArray<TEnum, TValue, EnumReferenceValue<TEnum, TValue>>
		where TValue : class
		where TEnum : unmanaged, Enum {}

	public class OrderedEnumArray<TEnum, TValue, TEnumValue> : EnumArray<TEnum, TValue, TEnumValue>
		where TEnum : unmanaged, Enum
		where TEnumValue : IEnumValue<TEnum>, new()
	{
		public unsafe ref TEnumValue this[TEnum enumValue]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref values[GetIndexOf(enumValue)];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe int GetIndexOf(TEnum enumValue)
		{
			return *(int*)(&enumValue);
		}
	}
	[Serializable]
	public class MyClassContainer<TEnum, TValue> where TEnum : unmanaged, Enum
	{
		public EnumValue<TEnum, TValue>[] _values;
	}

	[Serializable]
	public class EnumArray<TEnum, TValue, TEnumValue> :
#if UNITY_EDITOR
		ISerializationCallbackReceiver,
#endif
		IEnumArray
		where TEnum : unmanaged, Enum
		where TEnumValue : IEnumValue<TEnum>
#if UNITY_EDITOR
		, new()
#endif
	{
#if UNITY_EDITOR
		private static readonly Array ENUM_VALUES = Enum.GetValues(typeof(TEnum));
#endif
		[SerializeField, HideLabel]
		protected TEnumValue[] values;

		public ref TEnumValue this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref values[index];
		}

		public int Lenght
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => values.Length;
		}

		protected virtual bool IsReorderable => false;

#if UNITY_EDITOR
		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			UpdateValues();
			OnValuesUpdated();
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			UpdateValues();
			OnValuesUpdated();
		}

		public unsafe void UpdateValues()
		{
			LazyInitialize();
			if (values.Length == ENUM_VALUES.Length)
			{
				for (var i = 0; i < values.Length; i++)
				{
					var enumName = values[i].EnumValueName;
					if (!Enum.TryParse<TEnum>(enumName, out var enumValue) || !values[i].EnumValue.Equals(enumValue))
						goto update;
				}
				if (!IsReorderable)
					Array.Sort(values);
				return;
			}
			update:

			var valuesNew = new List<TEnumValue>(ENUM_VALUES.Length);
			var hashSet = new HashSet<TEnum>();

			var isValid = stackalloc bool[values.Length];

			for (var i = 0; i < values.Length; i++)
			{
				var enumName = values[i].EnumValueName;
				if (Enum.TryParse<TEnum>(enumName, out var enumValue) && !hashSet.Contains(enumValue))
				{
					values[i].EnumValue = enumValue;
					hashSet.Add(values[i].EnumValue);

					isValid[i] = true;
				}
				else
				{
					isValid[i] = false;
				}
			}
			for (var i = 0; i < values.Length; i++)
			{
				if (isValid[i])
				{
					valuesNew.Add(values[i]);
				}
				else if (Enum.IsDefined(typeof(TEnum), values[i].EnumValue) && !hashSet.Contains(values[i].EnumValue))
				{
					values[i].EnumValueName = Enum.GetName(typeof(TEnum), values[i].EnumValue);

					valuesNew.Add(values[i]);
					hashSet.Add(values[i].EnumValue);
				}
			}

			foreach (TEnum enumValue in ENUM_VALUES)
			{
				if (hashSet.Contains(enumValue))
					continue;
				valuesNew.Add(new TEnumValue
				{
					EnumValue = enumValue,
					EnumValueName = Enum.GetName(typeof(TEnum), enumValue),
				});
			}

			values = valuesNew.ToArray();

			if (!IsReorderable)
				Array.Sort(values);
		}

		protected virtual void OnValuesUpdated() {}

		protected virtual void LazyInitialize()
		{
			if (values != null)
				return;

			values = new TEnumValue[ENUM_VALUES.Length];

			for (var i = 0; i < ENUM_VALUES.Length; i++)
			{
				values[i] = new TEnumValue
				{
					EnumValue = (TEnum)ENUM_VALUES.GetValue(i),
				};
			}
		}
#endif
	}

	public interface IEnumArray {}

	[Serializable]
	public struct EnumValue<TEnum, TValue> : IComparable<EnumValue<TEnum, TValue>>, IEnumValue<TEnum>
		where TEnum: unmanaged, Enum
	{
		[SerializeField, HideInInspector]
		private string enumValueName;

		[Readonly, HideLabel, DrawOffset(xOffset = -15f)]
		public TEnum enumValue;
		[Label(""), DrawOffset(offsetLines = -1, foldoutIndent = 1, xOffset = -14f, disableIfHasSubBody = true)]
		public TValue value;

		TEnum IEnumValue<TEnum>.EnumValue
		{
			get => enumValue;
			set => enumValue = value;
		}

		string IEnumValue<TEnum>.EnumValueName
		{
			get => enumValueName;
			set => enumValueName = value;
		}

		public int CompareTo(EnumValue<TEnum, TValue> other)
		{
			return enumValue.CompareTo(other.enumValue);
		}
	}

	[Serializable]
	public struct EnumReferenceValue<TEnum, TValue> : IComparable<EnumReferenceValue<TEnum, TValue>>, IEnumValue<TEnum>
		where TEnum: unmanaged, Enum
		where TValue : class
	{
		[SerializeField, HideInInspector]
		private string enumValueName;

		[Readonly, HideLabel, DrawOffset(xOffset = -16f)]
		public TEnum enumValue;
		[HideLabel]
		[SerializeReference, ReferenceSelection]
		public TValue value;

		TEnum IEnumValue<TEnum>.EnumValue
		{
			get => enumValue;
			set => enumValue = value;
		}

		string IEnumValue<TEnum>.EnumValueName
		{
			get => enumValueName;
			set => enumValueName = value;
		}

		public int CompareTo(EnumReferenceValue<TEnum, TValue> other)
		{
			return enumValue.CompareTo(other.enumValue);
		}
	}

	public interface IEnumValue<TEnum>
	{
		public TEnum EnumValue { get; set; }
		public string EnumValueName { get; set; }
	}

	public struct EmptyStruct{}
}