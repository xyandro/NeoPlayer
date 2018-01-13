using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoPlayer.Models;

namespace NeoPlayer
{
	partial class ShortcutsDialog
	{
		static DependencyProperty ShortcutsListProperty = DependencyProperty.Register(nameof(ShortcutsList), typeof(ObservableCollection<Shortcut>), typeof(ShortcutsDialog));

		ObservableCollection<Shortcut> ShortcutsList { get { return (ObservableCollection<Shortcut>)GetValue(ShortcutsListProperty); } set { SetValue(ShortcutsListProperty, value); } }

		List<Shortcut> initial;
		ShortcutsDialog()
		{
			InitializeComponent();
			DataContext = this;
			initial = Database.GetAsync<Shortcut>().Result;
			ShortcutsList = new ObservableCollection<Shortcut>(initial.Select(shortcut => Helpers.Copy(shortcut)).OrderBy(shortcut => shortcut.Name));
		}

		void OnDeleteClick(object sender, RoutedEventArgs e) => ShortcutsList.Remove((sender as Button).Tag as Shortcut);

		void OnAddClick(object sender, RoutedEventArgs e) => ShortcutsList.Add(new Shortcut());

		void OnOKClick(object sender, RoutedEventArgs e)
		{
			var deleteIDs = initial.Select(shortcut => shortcut.ShortcutID).Except(ShortcutsList.Select(shortcut => shortcut.ShortcutID)).ToList();
			foreach (var shortcutID in deleteIDs)
				Database.DeleteAsync<Shortcut>(shortcutID).Wait();

			var oldShortcuts = initial.ToDictionary(shortcut => shortcut.ShortcutID);
			var updateShortcuts = ShortcutsList.Where(shortcut => (shortcut.ShortcutID == 0) || (!Helpers.Match(shortcut, oldShortcuts[shortcut.ShortcutID]))).ToList();
			foreach (var shortcut in updateShortcuts)
				Database.AddOrUpdateAsync(shortcut).Wait();

			DialogResult = true;
		}

		static public void Run() => new ShortcutsDialog().ShowDialog();
	}
}
