using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using NeoPlayer;

namespace NeoPlayerTestClient
{
	partial class MainWindow
	{
		public static MainWindow Current { get; private set; }

		public static DependencyProperty SearchTextProperty = DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(MainWindow));
		public static DependencyProperty CurrentSongProperty = DependencyProperty.Register(nameof(CurrentSong), typeof(string), typeof(MainWindow));
		public static DependencyProperty PositionProperty = DependencyProperty.Register(nameof(Position), typeof(int), typeof(MainWindow));
		public static DependencyProperty LengthProperty = DependencyProperty.Register(nameof(Length), typeof(int), typeof(MainWindow));
		public static DependencyProperty QueuedProperty = DependencyProperty.Register(nameof(Queued), typeof(ObservableCollection<MediaData>), typeof(MainWindow));

		public string SearchText { get { return (string)GetValue(SearchTextProperty); } set { SetValue(SearchTextProperty, value); } }
		public string CurrentSong { get { return (string)GetValue(CurrentSongProperty); } set { SetValue(CurrentSongProperty, value); } }
		public int Position { get { return (int)GetValue(PositionProperty); } set { SetValue(PositionProperty, value); } }
		public int Length { get { return (int)GetValue(LengthProperty); } set { SetValue(LengthProperty, value); } }
		public ObservableCollection<MediaData> Queued { get { return (ObservableCollection<MediaData>)GetValue(QueuedProperty); } set { SetValue(QueuedProperty, value); } }

		public MainWindow()
		{
			Current = this;

			InitializeComponent();
			SearchText = "My Search Text";
			CurrentSong = "Current song";
			Position = 125;
			Length = 255;
			Queued = new ObservableCollection<MediaData>();

			NetClient.RunSocket();
		}

		public void SetQueued(IEnumerable<MediaData> mediaData)
		{
			Queued = new ObservableCollection<MediaData>(mediaData);
		}
	}

	public class SecondsToTimeConverter : MarkupExtension, IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((value == null) || (!(value is int)))
				return DependencyProperty.UnsetValue;
			return TimeSpan.FromSeconds((int)value).ToString(@"m\:ss");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
