using System;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;

using Foundation;
using AppKit;
using CoreGraphics;

using Xamarin.PropertyEditing.ViewModels;
using Xamarin.PropertyEditing.Mac.Resources;

namespace Xamarin.PropertyEditing.Mac
{
	internal class PredefinedValuesEditor<T> : PropertyEditorControl<T>
		where T : struct
	{
		internal new PredefinedValuesViewModel<T> ViewModel
		{
			get { return (PredefinedValuesViewModel<T>)base.ViewModel; }
			set { base.ViewModel = value; }
		}

		public PredefinedValuesEditor ()
		{
			base.TranslatesAutoresizingMaskIntoConstraints = false;

			this.comboBox = new NSComboBox {
				TranslatesAutoresizingMaskIntoConstraints = false,
				BackgroundColor = NSColor.Clear,
				StringValue = String.Empty,
				ControlSize = NSControlSize.Small,
				Editable = false,
				Font = NSFont.FromFontName(DefaultFontName, DefaultFontSize),
			};

			this.comboBox.SelectionChanged += (sender, e) => {
				ViewModel.ValueName = comboBox.SelectedValue.ToString ();
			};

			this.popUpButton = new NSPopUpButton {
				TranslatesAutoresizingMaskIntoConstraints = false,
				StringValue = String.Empty,
				ControlSize = NSControlSize.Small,
				Font = NSFont.FromFontName (DefaultFontName, DefaultFontSize),
			};

			popupButtonList = new NSMenu ();
			popUpButton.Menu = popupButtonList;

			popUpButton.Activated += (o, e) => {
				ViewModel.ValueName = (o as NSPopUpButton).Title;
			};

			UpdateTheme ();
		}

		public override NSView FirstKeyView => this.firstKeyView;
		public override NSView LastKeyView => this.lastKeyView;

		readonly NSComboBox comboBox;
		readonly NSPopUpButton popUpButton;
		private NSMenu popupButtonList;

		private bool dataPopulated;
		private NSView firstKeyView;
		private NSView lastKeyView;

		protected override void HandleErrorsChanged (object sender, DataErrorsChangedEventArgs e)
		{
			UpdateErrorsDisplayed (ViewModel.GetErrors (e.PropertyName));
		}

		protected override void SetEnabled ()
		{
			if (ViewModel.IsConstrainedToPredefined) {
				this.popUpButton.Enabled = ViewModel.Property.CanWrite;
			} else {
				this.comboBox.Enabled = ViewModel.Property.CanWrite;
			}
		}

		protected override void UpdateErrorsDisplayed (IEnumerable errors)
		{
			if (ViewModel.HasErrors) {
				SetErrors (errors);
			} else {
				SetErrors (null);
				SetEnabled ();
			}
		}

		protected override void OnViewModelChanged (PropertyViewModel oldModel)
		{
			if (!this.dataPopulated) {
				if (ViewModel.IsConstrainedToPredefined) {
					this.popupButtonList.RemoveAllItems ();
					foreach (var item in ViewModel.PossibleValues) {
						this.popupButtonList.AddItem (new NSMenuItem (item));
					}

					AddSubview (this.popUpButton);

					this.DoConstraints (new[] {
						this.popUpButton.ConstraintTo (this, (pub, c) => pub.Width == c.Width - 34),
						this.popUpButton.ConstraintTo (this, (pub, c) => pub.Height == DefaultControlHeight + 1),
						this.popUpButton.ConstraintTo (this, (pub, c) => pub.Left == pub.Left + 4),
						this.popUpButton.ConstraintTo (this, (pub, c) => pub.Top == pub.Top + 0),
					});

					this.firstKeyView = this.popUpButton;
					this.lastKeyView = this.popUpButton;
				} else {
					this.comboBox.RemoveAll ();

					// Once the VM is loaded we need a one time population
					foreach (var item in ViewModel.PossibleValues) {
						this.comboBox.Add (new NSString (item));
					}

					AddSubview (this.comboBox);

					this.DoConstraints (new[] {
						this.comboBox.ConstraintTo (this, (cb, c) => cb.Width == c.Width - 35),
						this.comboBox.ConstraintTo (this, (cb, c) => cb.Height == DefaultControlHeight),
						this.comboBox.ConstraintTo (this, (cb, c) => cb.Left == cb.Left + 4),
						this.comboBox.ConstraintTo (this, (cb, c) => cb.Top == cb.Top + 0),
					});

					this.firstKeyView = this.comboBox;
					this.lastKeyView = this.comboBox;
				}

				this.dataPopulated = true;
			}

			base.OnViewModelChanged (oldModel);
		}

		protected override void UpdateValue ()
		{
			if (ViewModel.IsConstrainedToPredefined) {
				this.popUpButton.Title = ViewModel.ValueName ?? String.Empty;
			} else {
				this.comboBox.StringValue = ViewModel.ValueName ?? String.Empty;
			}
		}

		protected override void UpdateAccessibilityValues ()
		{
			if (ViewModel.IsConstrainedToPredefined) {
				this.popUpButton.AccessibilityEnabled = this.popUpButton.Enabled;
				this.popUpButton.AccessibilityTitle = string.Format (LocalizationResources.AccessibilityCombobox, ViewModel.Property.Name);
			} else {
				this.comboBox.AccessibilityEnabled = this.comboBox.Enabled;
				this.comboBox.AccessibilityTitle = string.Format (LocalizationResources.AccessibilityCombobox, ViewModel.Property.Name);
			}
		}
	}
}
