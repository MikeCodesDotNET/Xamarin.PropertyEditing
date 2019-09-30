using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

using CoreGraphics;
using Foundation;
using AppKit;

using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Mac
{
	public partial class PropertyEditorPanel : NSView
	{
		public PropertyEditorPanel ()
		{
			this.hostResources = new HostResourceProvider ();
			Initialize ();
		}

		public PropertyEditorPanel (IHostResourceProvider hostResources)
		{
			this.hostResources = hostResources;
			Initialize ();
		}

		// Called when created from unmanaged code
		public PropertyEditorPanel (IntPtr handle) : base (handle)
		{
			this.hostResources = new HostResourceProvider ();
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public PropertyEditorPanel (NSCoder coder) : base (coder)
		{
			this.hostResources = new HostResourceProvider ();
			Initialize ();
		}

		public bool ShowHeader
		{
			get => this.propertyList.ShowHeader;
			set => this.propertyList.ShowHeader = value;
		}

		public PropertyArrangeMode ArrangeMode
		{
			get => this.viewModel.ArrangeMode;
			set => this.viewModel.ArrangeMode = value;
		}

		public bool IsArrangeEnabled
		{
			get { return this.isArrangeEnabled; }
			set
			{
				if (this.isArrangeEnabled == value)
					return;

				this.isArrangeEnabled = value;
			}
		}

		public IHostResourceProvider HostResourceProvider
		{
			get => this.hostResources;
			set
			{
				if (this.hostResources == value)
					return;
				if (value == null)
					throw new ArgumentNullException (nameof (value), "Cannot set HostResourceProvider to null");

				this.hostResources = value;
				UpdateResourceProvider ();
			}
		}

		public TargetPlatform TargetPlatform
		{
			get { return this.targetPlatform; }
			set
			{
				if (this.viewModel != null) {
					this.viewModel.PropertyChanged -= OnVmPropertyChanged;

					var views = this.tabStack.Views;
					for (int i = 0; i < views.Length; i++) {
						var button = (TabButton)views[i];
						button.Clicked -= OnArrangeModeChanged;
						button.RemoveFromSuperview ();
					}
				}

				this.targetPlatform = value;
				this.viewModel = (value != null) ? new PanelViewModel (value) : null;
				this.propertyList.ViewModel = this.viewModel;

				OnVmPropertyChanged (this.viewModel, new PropertyChangedEventArgs (null));
				if (this.viewModel != null) {
					this.viewModel.PropertyChanged += OnVmPropertyChanged;

					for (int i = 0; i < this.viewModel.ArrangeModes.Count; i++) {
						var item = this.viewModel.ArrangeModes[i];
						string imageName = GetIconName (item.ArrangeMode);
						TabButton arrangeMode = new TabButton (this.hostResources, imageName) {
							Bounds = new CGRect (0, 0, 32, 30),
							Tag = i,
							Selected = item.IsChecked
						};

						arrangeMode.Clicked += OnArrangeModeChanged;

						this.tabStack.AddView (arrangeMode, NSStackViewGravity.Top);
					}
				}
			}
		}

		public ICollection<object> SelectedItems => this.viewModel.SelectedObjects;

		public void Select (IEnumerable<object> selectedItems)
		{
			if (selectedItems == null)
				throw new ArgumentNullException (nameof (selectedItems));

			((ObservableCollectionEx<object>)SelectedItems).Reset (selectedItems);
		}

		private IHostResourceProvider hostResources;
		private bool isArrangeEnabled = true;
		private TargetPlatform targetPlatform;
		private PropertyList propertyList;
		private PanelViewModel viewModel;

		private NSSearchField propertyFilter;
		private NSStackView tabStack;
		private DynamicBox header, border;

		private void Initialize ()
		{
			AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;

			this.header = new DynamicBox (HostResourceProvider, NamedResources.PanelTabBackground) {
				ContentViewMargins = new CGSize (0, 0),
				ContentView = new NSView ()
			};
			AddSubview (this.header);

			this.border = new DynamicBox (HostResourceProvider, NamedResources.TabBorderColor);
			header.AddSubview (this.border);

			this.propertyFilter = new NSSearchField {
				PlaceholderString = Properties.Resources.PropertyFilterLabel,
				TranslatesAutoresizingMaskIntoConstraints = false,
			};
			((NSView)this.header.ContentView).AddSubview (this.propertyFilter);

			this.propertyFilter.Changed += OnPropertyFilterChanged;

			this.tabStack = new NSStackView {
				Orientation = NSUserInterfaceLayoutOrientation.Horizontal,
				TranslatesAutoresizingMaskIntoConstraints = false,
				EdgeInsets = new NSEdgeInsets (0, 0, 0, 0)
			};

			((NSView)this.header.ContentView).AddSubview (this.tabStack);

			this.propertyList = new PropertyList {
				HostResourceProvider = this.hostResources,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			AddSubview (this.propertyList);

			var propertyPanelWidth = (nfloat)Math.Max (Frame.Width, 1);
			headerWidthConstraint = NSLayoutConstraint.Create (this.header, NSLayoutAttribute.Width, NSLayoutRelation.Equal, 1, propertyPanelWidth);
			propertyListWidthConstraint = NSLayoutConstraint.Create (this.propertyList, NSLayoutAttribute.Width, NSLayoutRelation.Equal, 1, propertyPanelWidth);
			propertyListHeightConstraint = NSLayoutConstraint.Create (this.propertyList, NSLayoutAttribute.Height, NSLayoutRelation.Equal, 1, (nfloat)Math.Max (Frame.Height, 1));


			this.AddConstraints (new[] {
				NSLayoutConstraint.Create (this.header, NSLayoutAttribute.Left, NSLayoutRelation.Equal, this, NSLayoutAttribute.Left, 1, 0),
				NSLayoutConstraint.Create (this.header, NSLayoutAttribute.Top, NSLayoutRelation.Equal, this, NSLayoutAttribute.Top, 1, 0),
				headerWidthConstraint,

				NSLayoutConstraint.Create (this.tabStack, NSLayoutAttribute.Left, NSLayoutRelation.Equal, this.header,  NSLayoutAttribute.Left, 1, 0),
				NSLayoutConstraint.Create (this.tabStack, NSLayoutAttribute.Top, NSLayoutRelation.Equal, this.header, NSLayoutAttribute.Top, 1, 0),
				NSLayoutConstraint.Create (this.tabStack, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, this.header, NSLayoutAttribute.Bottom, 1, 0),

				NSLayoutConstraint.Create (this.propertyFilter, NSLayoutAttribute.Right, NSLayoutRelation.Equal, this.header, NSLayoutAttribute.Right, 1, -15),
				NSLayoutConstraint.Create (this.propertyFilter, NSLayoutAttribute.Width, NSLayoutRelation.Equal, 1, 150),
				NSLayoutConstraint.Create (this.propertyFilter, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, this.header, NSLayoutAttribute.CenterY, 1, 0),

				NSLayoutConstraint.Create (this.border, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, this.header, NSLayoutAttribute.Bottom, 1, 0),
				NSLayoutConstraint.Create (this.border, NSLayoutAttribute.Width, NSLayoutRelation.Equal, this.header, NSLayoutAttribute.Width, 1, 0),
				NSLayoutConstraint.Create (this.border, NSLayoutAttribute.Height, NSLayoutRelation.Equal, this.header, NSLayoutAttribute.Height, 1, 0),

				NSLayoutConstraint.Create (this.propertyList, NSLayoutAttribute.Top, NSLayoutRelation.Equal, border, NSLayoutAttribute.Bottom, 1, 0),
				propertyListWidthConstraint,
				propertyListHeightConstraint
			});

			UpdateResourceProvider ();
		}

		const float HeaderHeight = 30;
		NSLayoutConstraint headerWidthConstraint, propertyListWidthConstraint, propertyListHeightConstraint;

		public override void SetFrameSize (CGSize newSize)
		{
			base.SetFrameSize (newSize);
			propertyListHeightConstraint.Constant = (nfloat)Math.Max (newSize.Height - HeaderHeight, 1);
		
			//our minimun size is tabStack + small margin + property filter
			var minimumSize = tabStack.Frame.Width + 30 + propertyFilter.Frame.Width;
			var calculatedWidth = (nfloat) Math.Max (minimumSize, newSize.Width);
			propertyListWidthConstraint.Constant = headerWidthConstraint.Constant = calculatedWidth;
		}

		public sealed override void ViewDidChangeEffectiveAppearance ()
		{
			base.ViewDidChangeEffectiveAppearance ();

			UpdateResourceProvider ();
		}

		private void UpdateResourceProvider ()
		{
			if (this.propertyList != null) {
				this.propertyList.HostResourceProvider = HostResourceProvider;
				this.header.HostResourceProvider = HostResourceProvider;
				this.border.HostResourceProvider = HostResourceProvider;
			}
		}

		private void OnArrangeModeChanged (object sender, EventArgs e)
		{
			this.viewModel.ArrangeMode = this.viewModel.ArrangeModes[(int)((NSView)sender).Tag].ArrangeMode;
		}

		private void OnPropertyFilterChanged (object sender, EventArgs e)
		{
			this.viewModel.FilterText = this.propertyFilter.Cell.Title;
			this.propertyList.UpdateExpansions ();
		}

		private void OnVmPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof (PanelViewModel.ArrangeMode) || String.IsNullOrEmpty (e.PropertyName)) {
				if (this.viewModel != null) {
					int selected = this.viewModel.ArrangeModes.Select (vm => vm.ArrangeMode).IndexOf (this.viewModel.ArrangeMode);
					var views = this.tabStack.Views;
					for (int i = 0; i < views.Length; i++) {
						((TabButton)views[i]).Selected = (i == selected);
					}
				}
			}
		}

		private string GetIconName (PropertyArrangeMode mode)
		{
			switch (mode) {
			case PropertyArrangeMode.Name:
				return "pe-sort-alphabetically-16";
			case PropertyArrangeMode.Category:
				return "pe-group-by-category-16";
			default:
				throw new ArgumentException();
			}
		}
	}
}
