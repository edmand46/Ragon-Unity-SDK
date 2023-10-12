using System;
using UnityEngine;

namespace Fusumity.Collections
{
	[Serializable]
	public struct ReferenceWrapper<TActual, TTarget> where TActual : class where TTarget: TActual
	{
		[SerializeReference]
		private TActual _value;

		public TActual Value => _value;

		public TTarget TargetValue
		{
			get => (TTarget)_value;
			set => _value = value;
		}

		public static implicit operator TActual(ReferenceWrapper<TActual, TTarget> wrapper)
		{
			return wrapper._value;
		}

		public static implicit operator TTarget(ReferenceWrapper<TActual, TTarget> wrapper)
		{
			return (TTarget)wrapper._value;
		}
	}

	[Serializable]
	public struct ReferenceWrapperWrapper<TActual, TTarget> where TActual : class where TTarget: TActual
	{
		public ReferenceWrapper<TActual, TTarget> _value;
	}
}
