using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Windows
{
	internal partial class CreateVariantWindow
		: WindowEx
	{
		public CreateVariantWindow (IEnumerable<ResourceDictionary> merged, IPropertyInfo property)
		{
			DataContext = new CreateVariantViewModel (property);
			InitializeComponent ();
			Resources.MergedDictionaries.AddItems (merged);
		}

		internal static PropertyVariation RequestVariant (FrameworkElement owner, IPropertyInfo property)
		{
			Window hostWindow = Window.GetWindow (owner);
			var w = new CreateVariantWindow (owner.Resources.MergedDictionaries, property) {
				Owner = hostWindow,
			};

			if (!w.ShowDialog () ?? false)
				return null;

			var vm = (CreateVariantViewModel)w.DataContext;
			return vm.Variation;
		}

		private void OnOkClicked (object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		private void OnCancelClicked (object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}
	}
}
