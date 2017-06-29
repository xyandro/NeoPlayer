using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoPlayerTestClient
{
	partial class MainWindow
	{
		public static DependencyProperty SearchTextProperty = DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(MainWindow));
		public static DependencyProperty CurrentSongProperty = DependencyProperty.Register(nameof(CurrentSong), typeof(string), typeof(MainWindow));
		public static DependencyProperty PositionProperty = DependencyProperty.Register(nameof(Position), typeof(int), typeof(MainWindow));
		public static DependencyProperty LengthProperty = DependencyProperty.Register(nameof(Length), typeof(int), typeof(MainWindow));
		public static DependencyProperty VideosProperty = DependencyProperty.Register(nameof(Videos), typeof(ObservableCollection<string>), typeof(MainWindow));

		public string SearchText { get { return (string)GetValue(SearchTextProperty); } set { SetValue(SearchTextProperty, value); } }
		public string CurrentSong { get { return (string)GetValue(CurrentSongProperty); } set { SetValue(CurrentSongProperty, value); } }
		public int Position { get { return (int)GetValue(PositionProperty); } set { SetValue(PositionProperty, value); } }
		public int Length { get { return (int)GetValue(LengthProperty); } set { SetValue(LengthProperty, value); } }
		public ObservableCollection<MediaData> Videos { get { return (ObservableCollection<MediaData>)GetValue(VideosProperty); } set { SetValue(VideosProperty, value); } }

		public MainWindow()
		{
			InitializeComponent();
			SearchText = "My Search Text";
			CurrentSong = "Current song";
			Position = 125;
			Length = 255;
			Videos = new ObservableCollection<MediaData>
			{
				new MediaData("Randon", "Randon Spackman", true),
				new MediaData("Ben", "Ben Christensen", true),
				new MediaData("Sophie", "Sophie Christensen", true),
				new MediaData("Timo", "Timo Christensen", false),
				new MediaData("Kate", "Kate Spackman", false),
				new MediaData("Phoebe", "Phoebe Christensen", true),
				new MediaData("Megan", "Megan Spackman", false),
				new MediaData("Randon", "Randon Spackman", true),
				new MediaData("Ben", "Ben Christensen", false),
				new MediaData("Sophie", "Sophie Christensen", false),
				new MediaData("Timo", "Timo Christensen", true),
				new MediaData("Kate", "Kate Spackman", false),
				new MediaData("Phoebe", "Phoebe Christensen", true),
				new MediaData("Megan", "Megan Spackman", false),
				new MediaData("Randon", "Randon Spackman", true),
				new MediaData("Ben", "Ben Christensen", false),
				new MediaData("Sophie", "Sophie Christensen", false),
				new MediaData("Timo", "Timo Christensen", true),
				new MediaData("Kate", "Kate Spackman", false),
				new MediaData("Phoebe", "Phoebe Christensen", true),
				new MediaData("Megan", "Megan Spackman", false),
				new MediaData("Randon", "Randon Spackman", false),
				new MediaData("Ben", "Ben Christensen", false),
				new MediaData("Sophie", "Sophie Christensen", false),
				new MediaData("Timo", "Timo Christensen", false),
				new MediaData("Kate", "Kate Spackman", false),
				new MediaData("Phoebe", "Phoebe Christensen", true),
				new MediaData("Megan", "Megan Spackman", false),
				new MediaData("Randon", "Randon Spackman", false),
				new MediaData("Ben", "Ben Christensen", true),
				new MediaData("Sophie", "Sophie Christensen", false),
				new MediaData("Timo", "Timo Christensen", false),
				new MediaData("Kate", "Kate Spackman", true),
				new MediaData("Phoebe", "Phoebe Christensen", true),
				new MediaData("Megan", "Megan Spackman", false),
			};

			NetClient.RunSocket();
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
