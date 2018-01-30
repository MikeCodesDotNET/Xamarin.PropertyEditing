using System;

namespace Xamarin.PropertyEditing
{
	public class Resource<T>
		: Resource
	{
		public Resource (ResourceSource source, string name, T value)
			: base (source, name)
		{
			Value = value;
		}

		public T Value
		{
			get;
		}

		public override Type RepresentationType => typeof(T);
	}

	public class Resource
	{
		public Resource (string name)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			Name = name;
		}

		public Resource (ResourceSource source, string name)
			: this (name)
		{			
			if (source == null)
				throw new ArgumentNullException (nameof (source));

			Source = source;
		}

		/// <summary>
		/// Gets the source for this resource.
		/// </summary>
		/// <remarks>This may be <c>null</c> when the resource is a dynamic reference that is unlocatable.</remarks>
		public ResourceSource Source
		{
			get;
		}

		/// <remarks>This may be <c>null</c> when the resource is a dynamic reference that is unlocatable.</remarks>
		public virtual Type RepresentationType => null;

		public string Name
		{
			get;
		}
	}
}
